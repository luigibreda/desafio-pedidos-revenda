using AutoMapper;
using BeverageDistributor.Application.DTOs.Order;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Enums;
using BeverageDistributor.Domain.Exceptions;
using BeverageDistributor.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace BeverageDistributor.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IDistributorRepository _distributorRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly ICacheService _cache;

        public OrderService(
            IOrderRepository orderRepository,
            IDistributorRepository distributorRepository,
            IMapper mapper,
            ILogger<OrderService> logger,
            ICacheService cache)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _distributorRepository = distributorRepository ?? throw new ArgumentNullException(nameof(distributorRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<OrderResponseDto> CreateAsync(CreateOrderDto createDto)
        {
            _logger.LogInformation("Criando novo pedido para distribuidor {DistributorId} e cliente {ClientId}", 
                createDto.DistributorId, createDto.ClientId);

            // Verifica o cache primeiro para o distribuidor
            var distributorCacheKey = $"distributor_{createDto.DistributorId}";
            var distributor = await _cache.GetOrCreateAsync(
                distributorCacheKey,
                async () => 
                {
                    _logger.LogDebug("Cache miss for distributor: {DistributorId}", createDto.DistributorId);
                    return await _distributorRepository.GetByIdAsync(createDto.DistributorId);
                },
                TimeSpan.FromMinutes(30));

            if (distributor == null)
            {
                _logger.LogWarning("Distribuidor com ID {DistributorId} não encontrado", createDto.DistributorId);
                throw new KeyNotFoundException($"Distribuidor com ID {createDto.DistributorId} não encontrado");
            }

            var order = new Order(createDto.DistributorId, createDto.ClientId);

            foreach (var itemDto in createDto.Items)
            {
                order.AddItem(itemDto.ProductId, itemDto.ProductName, itemDto.Quantity, itemDto.UnitPrice);
            }

            var createdOrder = await _orderRepository.AddAsync(order);
            _logger.LogInformation("Pedido {OrderId} criado com sucesso para o cliente {ClientId}", 
                createdOrder.Id, createdOrder.ClientId);

            // Invalida os caches relacionados
            _cache.Remove($"orders_distributor_{createDto.DistributorId}");
            _cache.Remove($"orders_client_{createDto.ClientId}");

            return _mapper.Map<OrderResponseDto>(createdOrder);
        }

        public async Task<OrderResponseDto> GetByIdAsync(Guid id)
        {
            var cacheKey = $"order_{id}";
            
            try
            {
                var orderDto = await _cache.GetOrCreateAsync(
                    cacheKey,
                    async () => 
                    {
                        _logger.LogDebug("Cache miss for order ID: {OrderId}", id);
                        var order = await _orderRepository.GetByIdAsync(id);
                        if (order == null)
                        {
                            _logger.LogWarning("Pedido com ID {OrderId} não encontrado", id);
                            throw new KeyNotFoundException($"Pedido com ID {id} não encontrado");
                        }
                        return _mapper.Map<OrderResponseDto>(order);
                    },
                    TimeSpan.FromMinutes(15)); // Cache por 15 minutos

                return orderDto;
            }
            catch (KeyNotFoundException)
            {
                // Remove do cache se o pedido não for encontrado para evitar cache de resultados negativos
                _cache.Remove(cacheKey);
                throw;
            }
        }

        public async Task<IEnumerable<OrderResponseDto>> GetByDistributorIdAsync(Guid distributorId)
        {
            var cacheKey = $"orders_distributor_{distributorId}";
            
            return await _cache.GetOrCreateAsync(
                cacheKey,
                async () => 
                {
                    _logger.LogDebug("Cache miss for distributor orders: {DistributorId}", distributorId);
                    var orders = await _orderRepository.GetByDistributorIdAsync(distributorId);
                    return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
                },
                TimeSpan.FromMinutes(10)); // Cache por 10 minutos
        }

        public async Task<IEnumerable<OrderResponseDto>> GetByClientIdAsync(string clientId)
        {
            var cacheKey = $"orders_client_{clientId}";
            
            return await _cache.GetOrCreateAsync(
                cacheKey,
                async () => 
                {
                    _logger.LogDebug("Cache miss for client orders: {ClientId}", clientId);
                    var orders = await _orderRepository.GetByClientIdAsync(clientId);
                    return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
                },
                TimeSpan.FromMinutes(10)); // Cache por 10 minutos
        }

        public async Task<OrderResponseDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto updateDto)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Pedido com ID {OrderId} não encontrado para atualização", id);
                throw new KeyNotFoundException($"Pedido com ID {id} não encontrado");
            }

            order.UpdateStatus(updateDto.Status);
            await _orderRepository.UpdateAsync(order);
            _logger.LogInformation("Status do pedido {OrderId} atualizado para {Status}", id, updateDto.Status);

            // Atualiza o cache para este pedido
            var cacheKey = $"order_{id}";
            _cache.Remove(cacheKey); // Remove o cache antigo
            
            // Invalida os caches de listagem
            _cache.Remove($"orders_distributor_{order.DistributorId}");
            _cache.Remove($"orders_client_{order.ClientId}");

            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Tentativa de excluir pedido inexistente: {OrderId}", id);
                return false;
            }

            // Remove o pedido do cache antes de excluir
            _cache.Remove($"order_{id}");
            _cache.Remove($"orders_distributor_{order.DistributorId}");
            _cache.Remove($"orders_client_{order.ClientId}");
            
            // Agora remove do repositório
            await _orderRepository.DeleteAsync(id);
            _logger.LogInformation("Pedido {OrderId} excluído com sucesso", id);

            return true;
        }

        // O mapeamento agora é feito diretamente pelo AutoMapper através do perfil de mapeamento
    }
}
