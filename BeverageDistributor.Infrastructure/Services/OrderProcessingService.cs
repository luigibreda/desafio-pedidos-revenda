using System;
using System.Collections.Generic;
using System.Linq;
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
    public class OrderProcessingService : BackgroundService, IAsyncDisposable
    {
        private IConnection? _connection;
        private IModel? _channel;
        private readonly ILogger<OrderProcessingService> _logger;
        private readonly IExternalOrderService _externalOrderService;
        private readonly RabbitMqSettings _rabbitMqSettings;
        private readonly OrderProcessingSettings _orderProcessingSettings;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        
        // Nomes das filas e exchanges
        private const string MainQueueName = "order_processing";
        private const string RetryQueueName = "order_processing_retry";
        private const string DeadLetterQueueName = "order_processing_dead_letter";
        private const string MainExchangeName = "order_exchange";
        private const string RetryExchangeName = "order_retry_exchange";
        private const string DeadLetterExchangeName = "order_dead_letter_exchange";
        
        private bool _disposed;
        private bool _initialized;

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
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _connectionLock.WaitAsync();
            try
            {
                if (_initialized) return;

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

                _channel.ExchangeDeclare(MainExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
                _channel.ExchangeDeclare(RetryExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
                _channel.ExchangeDeclare(DeadLetterExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
                var mainQueueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", RetryExchangeName },
                    { "x-message-ttl", 30000 }
                };

                var retryQueueArgs = new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", MainExchangeName },
                    { "x-message-ttl", 30000 },
                    { "x-dead-letter-routing-key", MainQueueName }
                };

                _channel.QueueDeclare(
                    queue: MainQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: mainQueueArgs);

                _channel.QueueDeclare(
                    queue: RetryQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: retryQueueArgs);

                _channel.QueueDeclare(
                    queue: DeadLetterQueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _channel.QueueBind(MainQueueName, MainExchangeName, MainQueueName);
                _channel.QueueBind(RetryQueueName, RetryExchangeName, RetryQueueName);
                _channel.QueueBind(DeadLetterQueueName, DeadLetterExchangeName, DeadLetterQueueName);

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _connection.ConnectionShutdown += (sender, e) =>
                {
                    _logger.LogWarning("Conexão com RabbitMQ encerrada: {ReplyText}", e.ReplyText);
                    _initialized = false;
                };

                _initialized = true;
                _logger.LogInformation("Conexão com RabbitMQ estabelecida e filas configuradas.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar a conexão com o RabbitMQ");
                throw;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;
        
        private IModel Channel => _channel ?? throw new InvalidOperationException("RabbitMQ channel is not initialized");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!IsConnected)
                    {
                        await EnsureInitializedAsync();
                    }

var consumer = new AsyncEventingBasicConsumer(Channel);
                    
                    consumer.Received += async (model, ea) =>
            {
                string messageId = null;
                int retryCount = 0;
                
                try
                {
                    messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
                    retryCount = GetRetryCount(ea.BasicProperties.Headers);
                    
                    _logger.LogInformation("Processando mensagem {MessageId} (Tentativa {RetryCount})", 
                        messageId, retryCount + 1);

                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var orderRequest = JsonSerializer.Deserialize<ExternalOrderRequestDto>(message);

                    ValidateOrderRequest(orderRequest, _orderProcessingSettings.MinOrderQuantity);

                    var response = await _externalOrderService.SubmitOrderAsync(orderRequest);

                    _logger.LogInformation("Pedido processado com sucesso. OrderId: {OrderId}", response.OrderId);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem {MessageId} (Tentativa {RetryCount})", 
                        messageId, retryCount + 1);

                    if (retryCount >= _orderProcessingSettings.MaxRetryAttempts)
                    {
                        // Máximo de tentativas atingido, enviar para DLQ
                        _logger.LogWarning("Máximo de tentativas atingido para mensagem {MessageId}. Enviando para DLQ.", messageId);
                        PublishToDeadLetterQueue(ea, messageId, ex);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        // Rejeitar a mensagem para ir para a fila de retentativa
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
                    };

Channel.BasicConsume(queue: MainQueueName,
                                   autoAck: false,
                                   consumer: consumer);

                    _logger.LogInformation("Consumidor registrado na fila {QueueName}", MainQueueName);
                    
                    // Manter o consumidor ativo
                    while (IsConnected && !stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Erro no consumidor. Tentando reconectar em 5 segundos...");
                    _initialized = false;
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }

        private void ValidateOrderRequest(ExternalOrderRequestDto orderRequest, int minOrderQuantity)
        {
            if (orderRequest == null)
                throw new OrderValidationException("Pedido não pode ser nulo");

            if (string.IsNullOrWhiteSpace(orderRequest.DistributorId))
                throw new OrderValidationException("O ID do distribuidor é obrigatório");

            if (orderRequest.Items == null || orderRequest.Items.Count == 0)
                throw new OrderValidationException("O pedido deve conter pelo menos um item");

            var totalQuantity = orderRequest.Items.Sum(item => item.Quantity);
            if (totalQuantity < minOrderQuantity)
                throw new OrderValidationException($"Quantidade total do pedido ({totalQuantity}) é menor que o mínimo exigido ({minOrderQuantity})");

            foreach (var item in orderRequest.Items)
            {
                if (string.IsNullOrWhiteSpace(item.ProductId))
                    throw new OrderValidationException("O ID do produto é obrigatório");

                if (string.IsNullOrWhiteSpace(item.ProductName))
                    throw new OrderValidationException("O nome do produto é obrigatório");

                if (item.Quantity <= 0)
                    throw new OrderValidationException($"A quantidade do produto {item.ProductId} deve ser maior que zero");

                if (item.UnitPrice < 0)
                    throw new OrderValidationException($"O preço unitário do produto {item.ProductId} não pode ser negativo");
            }
        }

        private int GetRetryCount(IDictionary<string, object> headers)
        {
            if (headers != null && headers.TryGetValue("x-retry-count", out var retryCountObj) && 
                int.TryParse(retryCountObj?.ToString(), out var retryCount))
            {
                return retryCount;
            }
            return 0;
        }

        private void PublishToDeadLetterQueue(BasicDeliverEventArgs ea, string messageId, Exception exception)
        {
            try
            {
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = messageId;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.Headers = new Dictionary<string, object>
                {
                    { "x-original-routing-key", ea.RoutingKey },
                    { "x-death-reason", exception.Message },
                    { "x-exception-type", exception.GetType().Name },
                    { "x-stack-trace", exception.StackTrace ?? string.Empty },
                    { "x-retry-count", GetRetryCount(ea.BasicProperties.Headers) + 1 }
                };

                _channel.BasicPublish(
                    exchange: DeadLetterExchangeName,
                    routingKey: DeadLetterQueueName,
                    mandatory: true,
                    basicProperties: properties,
                    body: ea.Body);

                _logger.LogWarning("Mensagem {MessageId} enviada para DLQ. Motivo: {ErrorMessage}", 
                    messageId, exception.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar mensagem {MessageId} para DLQ", messageId);
            }
        }



        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            try
            {
                if (_channel?.IsOpen == true)
                {
                    _channel.Close();
                    _channel.Dispose();
                }
                _channel?.Dispose();
                _channel = null;

                if (_connection?.IsOpen == true)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                _connection?.Dispose();
                _connection = null;
                
                _connectionLock?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao liberar recursos do RabbitMQ");
            }
        }
        
        public override void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class OrderValidationException : Exception
    {
        public OrderValidationException(string message) : base(message) { }
        public OrderValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
