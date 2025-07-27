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

        public OrderOrchestratorService(
            IOrderService orderService,
            IDistributorService distributorService,
            IMessageProducer messageProducer,
            ILogger<OrderOrchestratorService> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _distributorService = distributorService ?? throw new ArgumentNullException(nameof(distributorService));
            _messageProducer = messageProducer ?? throw new ArgumentNullException(nameof(messageProducer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderResponseDto> ProcessOrderAsync(CreateOrderDto createOrderDto, string distributorId)
        {
            if (createOrderDto == null)
                throw new ArgumentNullException(nameof(createOrderDto));

            if (string.IsNullOrWhiteSpace(distributorId))
                throw new ArgumentException("O ID do distribuidor é obrigatório", nameof(distributorId));

            // Garante que o distributorId do DTO corresponde ao fornecido
            createOrderDto.DistributorId = Guid.Parse(distributorId);

            try
            {
                _logger.LogInformation("Iniciando processamento de novo pedido para o cliente {ClientId}", createOrderDto.ClientId);

                // 1. Valida o distribuidor
                var distributor = await _distributorService.GetByIdAsync(createOrderDto.DistributorId);
                if (distributor == null)
                {
                    throw new ValidationException("Distribuidor não encontrado");
                }

                // 2. Cria um objeto Order para validação
                var order = new Order(createOrderDto.DistributorId, createOrderDto.ClientId);
                foreach (var item in createOrderDto.Items)
                {
                    order.AddItem(Guid.NewGuid(), item.ProductName, item.Quantity, item.UnitPrice);
                }

                // 3. Valida o pedido
                ValidateOrder(order);

                // 4. Salva o pedido no banco de dados
                var createdOrder = await _orderService.CreateAsync(createOrderDto);

                // 5. Prepara a mensagem para a fila
                var externalOrder = new ExternalOrderRequestDto
                {
                    DistributorId = distributorId,
                    Items = createOrderDto.Items.Select(item => new ExternalOrderItemDto
                    {
                        ProductId = item.ProductId.ToString(), // Convertendo para string
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    }).ToList()
                };

                // 6. Publica a mensagem na fila para processamento assíncrono
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

        private void ValidateOrder(Order order)
        {
            if (order.Items == null || !order.Items.Any())
            {
                throw new ValidationException("O pedido deve conter pelo menos um item");
            }

            // Valida cada item do pedido
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
        Task<OrderResponseDto> ProcessOrderAsync(CreateOrderDto createOrderDto, string distributorId);
    }

    public class OrderProcessingException : Exception
    {
        public OrderProcessingException(string message) : base(message) { }
        public OrderProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
