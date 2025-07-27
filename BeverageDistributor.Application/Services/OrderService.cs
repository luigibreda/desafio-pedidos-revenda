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

namespace BeverageDistributor.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IDistributorRepository _distributorRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IDistributorRepository distributorRepository,
            IMapper mapper,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _distributorRepository = distributorRepository ?? throw new ArgumentNullException(nameof(distributorRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderResponseDto> CreateAsync(CreateOrderDto createDto)
        {
            _logger.LogInformation("Creating new order for distributor {DistributorId} and client {ClientId}", 
                createDto.DistributorId, createDto.ClientId);

            // Verifica se o distribuidor existe
            var distributor = await _distributorRepository.GetByIdAsync(createDto.DistributorId);
            if (distributor == null)
            {
                _logger.LogWarning("Distributor with ID {DistributorId} not found", createDto.DistributorId);
                throw new KeyNotFoundException($"Distributor with ID {createDto.DistributorId} not found");
            }

            // Cria o pedido
            var order = new Order(createDto.DistributorId, createDto.ClientId);

            // Adiciona os itens ao pedido
            foreach (var itemDto in createDto.Items)
            {
                order.AddItem(
                    itemDto.ProductId,
                    itemDto.ProductName,
                    itemDto.Quantity,
                    itemDto.UnitPrice);
            }

            // Verifica se o pedido atende ao mínimo de 1000 unidades
            var totalItems = order.Items.Sum(i => i.Quantity);
            if (totalItems < 1000)
            {
                _logger.LogWarning("Order does not meet the minimum of 1000 units. Total units: {TotalUnits}", totalItems);
                throw new DomainException("The order must contain at least 1000 units in total.");
            }

            // Salva o pedido
            var createdOrder = await _orderRepository.AddAsync(order);
            _logger.LogInformation("Order created successfully with ID {OrderId}", createdOrder.Id);

            return MapToOrderResponseDto(createdOrder, distributor);
        }

        public async Task<OrderResponseDto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching order with ID {OrderId}", id);
            
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", id);
                throw new KeyNotFoundException($"Order with ID {id} not found");
            }

            return MapToOrderResponseDto(order, order.Distributor);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetByDistributorIdAsync(Guid distributorId)
        {
            _logger.LogInformation("Fetching orders for distributor {DistributorId}", distributorId);
            
            var orders = await _orderRepository.GetByDistributorIdAsync(distributorId);
            return orders.Select(o => MapToOrderResponseDto(o, o.Distributor));
        }

        public async Task<IEnumerable<OrderResponseDto>> GetByClientIdAsync(string clientId)
        {
            _logger.LogInformation("Fetching orders for client {ClientId}", clientId);
            
            var orders = await _orderRepository.GetByClientIdAsync(clientId);
            return orders.Select(o => MapToOrderResponseDto(o, o.Distributor));
        }

        public async Task<OrderResponseDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto updateDto)
        {
            _logger.LogInformation("Updating status for order {OrderId} to {Status}", id, updateDto.Status);
            
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found", id);
                throw new KeyNotFoundException($"Order with ID {id} not found");
            }

            // Atualiza o status do pedido com base no status fornecido
            switch (updateDto.Status.ToLower())
            {
                case "processing":
                    order.Process();
                    break;
                case "completed":
                    order.Complete();
                    break;
                case "cancelled":
                    order.Cancel();
                    break;
                default:
                    throw new DomainException($"Invalid status: {updateDto.Status}");
            }

            await _orderRepository.UpdateAsync(order);
            _logger.LogInformation("Order {OrderId} status updated to {Status}", id, order.Status);

            return MapToOrderResponseDto(order, order.Distributor);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting order with ID {OrderId}", id);
            
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found for deletion", id);
                return false;
            }

            // Apenas permite a exclusão de pedidos pendentes
            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning("Cannot delete order {OrderId} with status {Status}", id, order.Status);
                throw new DomainException("Only pending orders can be deleted");
            }

            await _orderRepository.DeleteAsync(id);
            _logger.LogInformation("Order {OrderId} deleted successfully", id);
            
            return true;
        }

        private OrderResponseDto MapToOrderResponseDto(Order order, Distributor distributor)
        {
            var dto = _mapper.Map<OrderResponseDto>(order);
            dto.DistributorName = distributor?.TradingName ?? string.Empty;
            dto.Status = order.Status.ToString();
            return dto;
        }
    }
}
