using System;
using System.Threading.Tasks;
using BeverageDistributor.Application.DTOs.Integration;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BeverageDistributor.API.Controllers
{
    /// <summary>
    /// Controlador para testes de integração com a API externa de pedidos.
    /// Este controlador NÃO deve ser usado em produção.
    /// </summary>
    /// <summary>
    /// Controlador para testes de integração com a API externa de pedidos.
    /// ATENÇÃO: Este controlador é destinado APENAS para ambientes de desenvolvimento e teste.
    /// NUNCA deve ser exposto em ambientes de produção.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class ExternalOrdersTestController : ControllerBase
    {
        private readonly IOrderOrchestratorService _orderOrchestrator;
        private readonly ILogger<ExternalOrdersTestController> _logger;
        private static readonly Random _random = new();

        public ExternalOrdersTestController(
            IOrderOrchestratorService orderOrchestrator,
            ILogger<ExternalOrdersTestController> logger)
        {
            _orderOrchestrator = orderOrchestrator ?? throw new ArgumentNullException(nameof(orderOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Envia um pedido de teste pré-definido para a API externa.
        /// </summary>
        /// <remarks>
        /// Exemplo de uso:
        /// 
        ///     POST /api/externalorderstest/submit-sample
        ///     
        /// Este endpoint cria e publica um pedido de teste na fila de processamento.
        /// <strong>ATENÇÃO:</strong> Este endpoint é apenas para fins de teste e demonstração.
        /// </remarks>
        /// <returns>Resultado da operação com detalhes do pedido processado.</returns>
        /// <response code="200">Pedido de teste publicado na fila com sucesso.</response>
        /// <response code="500">Erro ao processar o pedido de teste.</response>
        [HttpPost("submit-sample")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(object))]
        // [ApiExplorerSettings(Description = "Endpoint de teste para envio de pedidos para a API externa.")]
        public async Task<IActionResult> SubmitSampleOrderToExternalApi()
        {
            try
            {
                // Criar um pedido de teste
                var testOrder = new ExternalOrderRequestDto
                {
                    DistributorId = Guid.NewGuid().ToString(),
                    Items = new()
                    {
                        new()
                        {
                            ProductId = "PROD-001",
                            ProductName = "Cerveja Artesanal IPA",
                            Quantity = 1500,
                            UnitPrice = 12.99m
                        },
                        new()
                        {
                            ProductId = "PROD-002",
                            ProductName = "Refrigerante de Guaraná",
                            Quantity = 3500,
                            UnitPrice = 5.99m
                        }
                    }
                };

                _logger.LogInformation("[TESTE] Publicando pedido de teste na fila para processamento...");
                var messageId = await _orderOrchestrator.PublishOrderToQueueAsync(testOrder);
                _logger.LogInformation("[TESTE] Pedido publicado na fila com sucesso. MessageId: {MessageId}", messageId);

                return Ok(new
                {
                    Success = true,
                    Message = "Pedido publicado na fila para processamento assíncrono.",
                    MessageId = messageId,
                    Queue = "order_processing",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TESTE] Falha ao enviar pedido de teste para a API externa");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Falha no teste de integração com a API externa",
                    Error = ex.Message,
                    IsTest = true,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
