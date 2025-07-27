using BeverageDistributor.Application.DTOs.Order;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
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
        public async Task<ActionResult<OrderResponseDto>> Create([FromBody] CreateOrderDto createDto)
        {
            try
            {
                var order = await _orderService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain validation failed: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
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
