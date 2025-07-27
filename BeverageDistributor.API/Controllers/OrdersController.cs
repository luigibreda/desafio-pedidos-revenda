using BeverageDistributor.Application.DTOs.Order;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Application.Services;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponseDto>> GetById(Guid id)
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

        [HttpGet("distributor/{distributorId}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByDistributor(Guid distributorId)
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

        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByClient(string clientId)
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

        [HttpPost]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderResponseDto>> Create([FromBody] CreateOrderDto createDto)
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

        [HttpPut("{id}/status")]
        public async Task<ActionResult<OrderResponseDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto updateDto)
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

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
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
