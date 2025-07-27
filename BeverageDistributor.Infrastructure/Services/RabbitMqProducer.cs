using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BeverageDistributor.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using IModel = RabbitMQ.Client.IModel;

namespace BeverageDistributor.Infrastructure.Services
{
    public class RabbitMqProducer : IMessageProducer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqProducer> _logger;
        private readonly RabbitMqSettings _settings;
        private bool _disposed;

        public RabbitMqProducer(
            IOptions<RabbitMqSettings> settings,
            ILogger<RabbitMqProducer> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                _connection.ConnectionShutdown += (sender, e) =>
                {
                    _logger.LogWarning("Conexão com RabbitMQ encerrada: {ReplyText}", e.ReplyText);
                };

                _logger.LogInformation("Conectado ao RabbitMQ em {HostName}", _settings.HostName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar ao RabbitMQ");
                throw;
            }
        }

        public async Task PublishOrderAsync<T>(T message, string queueName) where T : class
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentException("O nome da fila não pode ser vazio", nameof(queueName));

            try
            {
                var arguments = new Dictionary<string, object>
                {
                    { "x-message-ttl", 30000 },
                    { "x-dead-letter-exchange", "order_retry_exchange" }
                };

                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,     
                    exclusive: false,  
                    autoDelete: false, 
                    arguments: arguments);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; 
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                _channel.BasicPublish(
                    exchange: string.Empty, 
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Mensagem publicada na fila {QueueName} com sucesso. MessageId: {MessageId}", 
                    queueName, properties.MessageId);
            }
            catch (OperationInterruptedException ex)
            {
                _logger.LogError(ex, "Erro ao publicar mensagem na fila {QueueName}", queueName);
                throw new MessageQueueException($"Erro ao publicar mensagem na fila {queueName}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao publicar mensagem na fila {QueueName}", queueName);
                throw new MessageQueueException($"Erro inesperado ao publicar mensagem na fila {queueName}", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
            }

            _disposed = true;
        }

        ~RabbitMqProducer()
        {
            Dispose(false);
        }
    }

    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
    }

    public class MessageQueueException : Exception
    {
        public MessageQueueException(string message) : base(message) { }
        public MessageQueueException(string message, Exception innerException) : base(message, innerException) { }
    }
}
