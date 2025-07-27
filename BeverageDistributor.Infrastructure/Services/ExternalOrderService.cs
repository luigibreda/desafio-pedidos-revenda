using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BeverageDistributor.Application.DTOs.Integration;
using BeverageDistributor.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace BeverageDistributor.Infrastructure.Services
{
    public class ExternalOrderService : IExternalOrderService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalOrderService> _logger;
        private readonly ExternalApiSettings _settings;

        public ExternalOrderService(
            HttpClient httpClient,
            IOptions<ExternalApiSettings> settings,
            ILogger<ExternalOrderService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<ExternalOrderResponseDto> SubmitOrderAsync(ExternalOrderRequestDto orderRequest)
        {
            if (orderRequest == null)
                throw new ArgumentNullException(nameof(orderRequest));

            try
            {
                var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
                    .AddRetry(new()
                    {
                        MaxRetryAttempts = 3,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout),
                        OnRetry = args =>
                        {
                            _logger.LogWarning(
                                "Tentativa {RetryAttempt} de envio do pedido à API externa. Motivo: {Outcome}",
                                args.AttemptNumber,
                                args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                            return ValueTask.CompletedTask;
                        }
                    })
                    .AddTimeout(TimeSpan.FromSeconds(30))
                    .AddCircuitBreaker(new()
                    {
                        FailureRatio = 0.5,
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        MinimumThroughput = 5,
                        BreakDuration = TimeSpan.FromMinutes(1),
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .HandleResult(r => (int)r.StatusCode >= 500),
                        OnOpened = args =>
                        {
                            _logger.LogWarning(
                                "Circuito aberto por {BreakDuration}ms devido a: {Outcome}",
                                args.BreakDuration.TotalMilliseconds, 
                                args.Outcome.Exception?.Message ?? args.Outcome.Result?.StatusCode.ToString());
                            return ValueTask.CompletedTask;
                        },
                        OnClosed = _ =>
                        {
                            _logger.LogInformation("Circuito fechado, as requisições serão permitidas novamente");
                            return ValueTask.CompletedTask;
                        },
                        OnHalfOpened = args =>
                        {
                            _logger.LogInformation("Testando se a API externa está respondendo...");
                            return ValueTask.CompletedTask;
                        }
                    })
                    .Build();

                var response = await pipeline.ExecuteAsync(async token =>
                {
                    var content = JsonContent.Create(orderRequest);
                    return await _httpClient.PostAsync(_settings.OrderEndpoint, content, token);
                }, CancellationToken.None);

                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                var orderResponse = JsonSerializer.Deserialize<ExternalOrderResponseDto>(
                    responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Pedido {OrderId} enviado com sucesso para a API externa", orderResponse?.OrderId);
                
                return orderResponse;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Não foi possível enviar o pedido. O circuito está aberto");
                throw new ExternalServiceUnavailableException("O serviço de pedidos externo está temporariamente indisponível. Tente novamente mais tarde.", ex);
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogError(ex, "Tempo limite excedido ao tentar enviar o pedido para a API externa");
                throw new ExternalServiceTimeoutException("O serviço de pedidos externo não respondeu a tempo. Tente novamente.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro ao enviar pedido para a API externa");
                throw new ExternalServiceException("Ocorreu um erro ao processar seu pedido. Por favor, tente novamente.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao enviar pedido para a API externa");
                throw new ExternalServiceException("Ocorreu um erro inesperado ao processar seu pedido.", ex);
            }
        }
    }

    public class ExternalApiSettings
    {
        public string BaseUrl { get; set; }
        public string OrderEndpoint { get; set; }
        public string ApiKey { get; set; }
    }

    public class ExternalServiceException : Exception
    {
        public ExternalServiceException(string message) : base(message) { }
        public ExternalServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ExternalServiceUnavailableException : ExternalServiceException
    {
        public ExternalServiceUnavailableException(string message) : base(message) { }
        public ExternalServiceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ExternalServiceTimeoutException : ExternalServiceException
    {
        public ExternalServiceTimeoutException(string message) : base(message) { }
        public ExternalServiceTimeoutException(string message, Exception innerException) : base(message, innerException) { }
    }
}
