using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BeverageDistributor.Application.DTOs.Integration;
using BeverageDistributor.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IModel = RabbitMQ.Client.IModel;

namespace BeverageDistributor.Infrastructure.Services
{
    public class OrderProcessingService : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<OrderProcessingService> _logger;
        private readonly IExternalOrderService _externalOrderService;
        private readonly RabbitMqSettings _rabbitMqSettings;
        private readonly OrderProcessingSettings _orderProcessingSettings;
        private readonly string _queueName = "order_processing";
        private bool _disposed;

        public OrderProcessingService(
            IOptions<RabbitMqSettings> rabbitMqSettings,
            IOptions<OrderProcessingSettings> orderProcessingSettings,
            IExternalOrderService externalOrderService,
            ILogger<OrderProcessingService> logger)
        {
            _rabbitMqSettings = rabbitMqSettings?.Value ?? throw new ArgumentNullException(nameof(rabbitMqSettings));
            _orderProcessingSettings = orderProcessingSettings?.Value ?? throw new ArgumentNullException(nameof(orderProcessingSettings));
            _externalOrderService = externalOrderService ?? throw new ArgumentNullException(nameof(externalOrderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _rabbitMqSettings.HostName,
                    Port = _rabbitMqSettings.Port,
                    UserName = _rabbitMqSettings.Username,
                    Password = _rabbitMqSettings.Password,
                    VirtualHost = _rabbitMqSettings.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _connection.ConnectionShutdown += (sender, e) =>
                {
                    _logger.LogWarning("Conexão com RabbitMQ encerrada: {ReplyText}", e.ReplyText);
                };

                _logger.LogInformation("Serviço de processamento de pedidos inicializado. Aguardando mensagens...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar o serviço de processamento de pedidos");
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                string messageId = null;
                try
                {
                    messageId = ea.BasicProperties.MessageId;
                    _logger.LogInformation("Processando mensagem {MessageId}", messageId);

                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var orderRequest = JsonSerializer.Deserialize<ExternalOrderRequestDto>(message);

                    ValidateOrderRequest(orderRequest, _orderProcessingSettings.MinOrderQuantity);

                    var response = await _externalOrderService.SubmitOrderAsync(orderRequest);

                    _logger.LogInformation("Pedido processado com sucesso. OrderId: {OrderId}", response.OrderId);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Erro ao desserializar a mensagem {MessageId}", messageId);
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (OrderValidationException ex)
                {
                    _logger.LogError(ex, "Validação falhou para a mensagem {MessageId}", messageId);
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar a mensagem {MessageId}", messageId);
                    
                    var redelivered = ea.BasicProperties.Headers != null && 
                                    ea.BasicProperties.Headers.ContainsKey("x-death") && 
                                    ((System.Collections.Generic.List<object>)ea.BasicProperties.Headers["x-death"])?.Count > 3;
                    if (redelivered)
                    {
                        _logger.LogWarning("Mensagem {MessageId} movida para a DLQ após várias tentativas", messageId);
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    else
                    {
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                }
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);

            await Task.CompletedTask;
        }

        private void ValidateOrderRequest(ExternalOrderRequestDto orderRequest, int minOrderQuantity)
        {
            if (orderRequest == null)
                throw new OrderValidationException("O pedido não pode ser nulo");

            if (string.IsNullOrWhiteSpace(orderRequest.DistributorId))
                throw new OrderValidationException("O ID do distribuidor é obrigatório");

            if (orderRequest.Items == null || orderRequest.Items.Count == 0)
                throw new OrderValidationException("O pedido deve conter pelo menos um item");

            foreach (var item in orderRequest.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId))
                    throw new OrderValidationException("O ID do produto é obrigatório");

                if (string.IsNullOrWhiteSpace(item.ProductName))
                    throw new OrderValidationException("O nome do produto é obrigatório");

                if (item.Quantity <= 0)
                    throw new OrderValidationException($"A quantidade do produto {item.ProductName} deve ser maior que zero");

                if (item.UnitPrice < 0)
                    throw new OrderValidationException($"O preço unitário do produto {item.ProductName} não pode ser negativo");
            }

            var totalUnits = 0;
            foreach (var item in orderRequest.Items)
            {
                totalUnits += item.Quantity;
            }

            if (totalUnits < minOrderQuantity)
                throw new OrderValidationException($"O pedido mínimo é de {minOrderQuantity} unidades. Total atual: {totalUnits} unidades");
        }

        public override void Dispose()
        {
            if (_disposed)
                return;

            _channel?.Close();
            _connection?.Close();
            base.Dispose();
            _disposed = true;
        }
    }

    public class OrderValidationException : Exception
    {
        public OrderValidationException(string message) : base(message) { }
        public OrderValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
