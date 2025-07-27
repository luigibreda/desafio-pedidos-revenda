using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BeverageDistributor.Application.DTOs.Order;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Application.DTOs.Integration;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BeverageDistributor.Application.Services
{
    public class OrderOrchestratorService : IOrderOrchestratorService
    {
        private readonly IOrderService _orderService;
        private readonly IDistributorService _distributorService;
        private readonly IMessageProducer _messageProducer;
        private readonly ILogger<OrderOrchestratorService> _logger;
        private readonly IExternalOrderService _externalOrderService;

        public OrderOrchestratorService(
            IOrderService orderService,
            IDistributorService distributorService,
            IMessageProducer messageProducer,
            IExternalOrderService externalOrderService,
            ILogger<OrderOrchestratorService> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _distributorService = distributorService ?? throw new ArgumentNullException(nameof(distributorService));
            _messageProducer = messageProducer ?? throw new ArgumentNullException(nameof(messageProducer));
            _externalOrderService = externalOrderService ?? throw new ArgumentNullException(nameof(externalOrderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderResponseDto> ProcessOrderAsync(CreateOrderDto createOrderDto, string distributorId)
        {
            if (createOrderDto == null)
                throw new ArgumentNullException(nameof(createOrderDto));

            if (string.IsNullOrWhiteSpace(distributorId))
                throw new ArgumentException("O ID do distribuidor é obrigatório", nameof(distributorId));

            createOrderDto.DistributorId = Guid.Parse(distributorId);

            try
            {
                _logger.LogInformation("Iniciando processamento de novo pedido para o cliente {ClientId}", createOrderDto.ClientId);

                var distributor = await _distributorService.GetByIdAsync(createOrderDto.DistributorId);
                if (distributor == null)
                {
                    throw new ValidationException("Distribuidor não encontrado");
                }

                var order = new Order(createOrderDto.DistributorId, createOrderDto.ClientId);
                foreach (var item in createOrderDto.Items)
                {
                    order.AddItem(Guid.NewGuid(), item.ProductName, item.Quantity, item.UnitPrice);
                }

                ValidateOrder(order);

                var totalQuantity = order.Items.Sum(item => item.Quantity);
                _logger.LogInformation("Validando quantidade total do pedido: {TotalQuantity} unidades", totalQuantity);
                
                if (totalQuantity < 1000)
                {
                    _logger.LogWarning("Pedido rejeitado: quantidade total {TotalQuantity} é menor que o mínimo de 1000 unidades", totalQuantity);
                    throw new ValidationException("O pedido deve conter no mínimo 1000 unidades no total");
                }
                
                _logger.LogInformation("Validação de quantidade mínima aprovada: {TotalQuantity} unidades", totalQuantity);

                var createdOrder = await _orderService.CreateAsync(createOrderDto);

                var externalOrder = new ExternalOrderRequestDto
                {
                    DistributorId = distributorId,
                    Items = createOrderDto.Items.Select(item => new ExternalOrderItemDto
                    {
                        ProductId = item.ProductId.ToString(),
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    }).ToList()
                };

                await _messageProducer.PublishOrderAsync(externalOrder, "order_processing");

                _logger.LogInformation("Pedido {OrderId} criado e enviado para processamento assíncrono", createdOrder.Id);

                return createdOrder;
            }
            catch (Exception ex) when (ex is not ValidationException)
            {
                _logger.LogError(ex, "Erro ao processar o pedido {OrderId}", createOrderDto.ClientId);
                throw new OrderProcessingException("Ocorreu um erro ao processar o pedido. Por favor, tente novamente mais tarde.", ex);
            }
        }

        public async Task<string> PublishOrderToQueueAsync(ExternalOrderRequestDto orderRequest)
        {
            if (orderRequest == null)
                throw new ArgumentNullException(nameof(orderRequest));

            try
            {
                _logger.LogInformation("Publicando pedido na fila de processamento...");
                
                if (orderRequest.Items == null || !orderRequest.Items.Any())
                {
                    throw new ValidationException("O pedido deve conter pelo menos um item");
                }

                foreach (var item in orderRequest.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new ValidationException($"A quantidade do produto {item.ProductName} deve ser maior que zero");
                    }

                    if (item.UnitPrice < 0)
                    {
                        throw new ValidationException($"O preço unitário do produto {item.ProductName} não pode ser negativo");
                    }
                }

                var messageId = Guid.NewGuid().ToString();
                await _messageProducer.PublishOrderAsync(orderRequest, "order_processing");
                
                _logger.LogInformation("Pedido publicado na fila com sucesso. MessageId: {MessageId}", messageId);
                
                return messageId;
            }
            catch (Exception ex) when (ex is not ValidationException)
            {
                _logger.LogError(ex, "Erro ao publicar pedido na fila");
                throw new OrderQueueException("Falha ao publicar o pedido na fila de processamento. Tente novamente mais tarde.", ex);
            }
        }

        private void ValidateOrder(Order order)
        {
            if (order.Items == null || !order.Items.Any())
            {
                throw new ValidationException("O pedido deve conter pelo menos um item");
            }

            foreach (var item in order.Items)
            {
                if (item.Quantity <= 0)
                {
                    throw new ValidationException($"A quantidade do produto {item.ProductName} deve ser maior que zero");
                }

                if (item.UnitPrice < 0)
                {
                    throw new ValidationException($"O preço unitário do produto {item.ProductName} não pode ser negativo");
                }
            }
        }
    }

    public interface IOrderOrchestratorService
    {
        /// <summary>
        /// Processa um pedido e o envia para a fila de processamento
        /// </summary>
        Task<OrderResponseDto> ProcessOrderAsync(CreateOrderDto createOrderDto, string distributorId);
        
        /// <summary>
        /// Publica um pedido diretamente na fila de processamento
        /// </summary>
        /// <param name="orderRequest">Dados do pedido a ser processado</param>
        /// <returns>ID da mensagem publicada na fila</returns>
        Task<string> PublishOrderToQueueAsync(ExternalOrderRequestDto orderRequest);
    }

    public class OrderProcessingException : Exception
    {
        public OrderProcessingException(string message) : base(message) { }
        public OrderProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class OrderQueueException : Exception
    {
        public OrderQueueException(string message) : base(message) { }
        public OrderQueueException(string message, Exception innerException) : base(message, innerException) { }
    }
}
