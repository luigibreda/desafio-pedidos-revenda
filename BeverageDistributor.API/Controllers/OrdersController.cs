using BeverageDistributor.Application.DTOs.Order;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Application.Services;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Controllers
{
    /// <summary>
    /// Controller responsável por gerenciar as operações relacionadas a pedidos de bebidas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IOrderOrchestratorService _orderOrchestrator;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            IOrderOrchestratorService orderOrchestrator,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _orderOrchestrator = orderOrchestrator ?? throw new ArgumentNullException(nameof(orderOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém os detalhes de um pedido específico pelo seu identificador único.
        /// </summary>
        /// <param name="id">O identificador único do pedido (GUID).</param>
        /// <returns>Retorna os detalhes do pedido.</returns>
        /// <response code="200">Retorna o pedido solicitado.</response>
        /// <response code="404">Pedido não encontrado.</response>
        /// <response code="500">Ocorreu um erro ao processar a solicitação.</response>
        [HttpGet("{id}", Name = "GetOrderById")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderResponseDto>> GetById(
            [Required(ErrorMessage = "O ID do pedido é obrigatório")]
            Guid id)
        {
            try
            {
                var order = await _orderService.GetByIdAsync(id);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Obtém todos os pedidos associados a um distribuidor específico.
        /// </summary>
        /// <param name="distributorId">O identificador único do distribuidor (GUID).</param>
        /// <returns>Retorna a lista de pedidos do distribuidor.</returns>
        /// <response code="200">Retorna a lista de pedidos com sucesso.</response>
        /// <response code="500">Ocorreu um erro ao processar a solicitação.</response>
        [HttpGet("distributor/{distributorId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrderResponseDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByDistributor(
            [Required(ErrorMessage = "O ID do distribuidor é obrigatório")]
            Guid distributorId)
        {
            try
            {
                var orders = await _orderService.GetByDistributorIdAsync(distributorId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders by distributor: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Obtém todos os pedidos associados a um cliente específico.
        /// </summary>
        /// <param name="clientId">O identificador único do cliente.</param>
        /// <returns>Retorna a lista de pedidos do cliente.</returns>
        /// <response code="200">Retorna a lista de pedidos com sucesso.</response>
        /// <response code="500">Ocorreu um erro ao processar a solicitação.</response>
        [HttpGet("client/{clientId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrderResponseDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByClient(
            [Required(ErrorMessage = "O ID do cliente é obrigatório")]
            string clientId)
        {
            try
            {
                var orders = await _orderService.GetByClientIdAsync(clientId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders by client: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Cria um novo pedido e inicia o processamento assíncrono.
        /// </summary>
        /// <param name="createDto">Dados necessários para criar um novo pedido.</param>
        /// <returns>Retorna os detalhes do pedido criado.</returns>
        /// <response code="201">Pedido criado com sucesso e enviado para processamento.</response>
        /// <response code="400">Dados inválidos fornecidos.</response>
        /// <response code="404">Distribuidor ou produto não encontrado.</response>
        /// <response code="500">Ocorreu um erro ao processar o pedido.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OrderResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<OrderResponseDto>> Create(
            [FromBody, Required(ErrorMessage = "Os dados do pedido são obrigatórios")]
            CreateOrderDto createDto)
        {
            try
            {
                _logger.LogInformation("Iniciando criação de pedido para o distribuidor {DistributorId}", createDto.DistributorId);
                
                // Cria o pedido usando o serviço de orquestração para processamento assíncrono
                var order = await _orderOrchestrator.ProcessOrderAsync(createDto, createDto.DistributorId.ToString());
                
                _logger.LogInformation("Pedido {OrderId} criado com sucesso e enviado para processamento", order.Id);
                
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Recurso não encontrado: {Message}", ex.Message);
                return NotFound(new ProblemDetails
                {
                    Title = "Recurso não encontrado",
                    Detail = ex.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao criar pedido: {Message}", ex.Message);
                return ValidationProblem(new ValidationProblemDetails
                {
                    Title = "Erro de validação",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Erro de domínio: {Message}", ex.Message);
                return BadRequest(new ProblemDetails
                {
                    Title = "Erro de domínio",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar o pedido");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro ao processar o pedido",
                    Detail = "Ocorreu um erro ao processar seu pedido. Por favor, tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Atualiza o status de um pedido existente.
        /// </summary>
        /// <param name="id">O identificador único do pedido (GUID).</param>
        /// <param name="updateDto">Dados para atualização do status do pedido.</param>
        /// <returns>Retorna os detalhes atualizados do pedido.</returns>
        /// <response code="200">Status do pedido atualizado com sucesso.</response>
        /// <response code="400">Dados inválidos fornecidos ou regra de negócio violada.</response>
        /// <response code="404">Pedido não encontrado.</response>
        /// <response code="500">Ocorreu um erro ao processar a solicitação.</response>
        [HttpPut("{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderResponseDto>> UpdateStatus(
            [Required(ErrorMessage = "O ID do pedido é obrigatório")] Guid id,
            [FromBody, Required(ErrorMessage = "Os dados de atualização de status são obrigatórios")]
            UpdateOrderStatusDto updateDto)
        {
            try
            {
                var order = await _orderService.UpdateStatusAsync(id, updateDto);
                return Ok(order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain validation failed: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Remove um pedido do sistema.
        /// </summary>
        /// <param name="id">O identificador único do pedido a ser removido (GUID).</param>
        /// <returns>Sem conteúdo em caso de sucesso.</returns>
        /// <response code="204">Pedido removido com sucesso.</response>
        /// <response code="400">Não foi possível remover o pedido devido a restrições de negócio.</response>
        /// <response code="404">Pedido não encontrado.</response>
        /// <response code="500">Ocorreu um erro ao processar a solicitação.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(
            [Required(ErrorMessage = "O ID do pedido é obrigatório")]
            Guid id)
        {
            try
            {
                var result = await _orderService.DeleteAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain validation failed: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
