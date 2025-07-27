using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeverageDistributor.Application.DTOs.Integration;
using BeverageDistributor.Application.Interfaces;
using Microsoft.Extensions.Logging;
using BeverageDistributor.Infrastructure.Services;

namespace BeverageDistributor.Infrastructure.Services
{
    public class MockExternalOrderService : IExternalOrderService
    {
        private readonly ILogger<MockExternalOrderService> _logger;
        private readonly Random _random = new();
        private bool _isServiceAvailable = true;
        private int _failureCount = 0;
        private const int MaxFailuresBeforeRecovery = 3;

        public MockExternalOrderService(ILogger<MockExternalOrderService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ExternalOrderResponseDto> SubmitOrderAsync(ExternalOrderRequestDto orderRequest)
        {
            if (orderRequest == null)
                throw new ArgumentNullException(nameof(orderRequest));

            _logger.LogInformation("Enviando pedido para a API externa. Itens: {ItemCount}", orderRequest.Items?.Count ?? 0);

            // Simulate random failures (10% chance)
            if (_random.Next(1, 11) == 1 && _isServiceAvailable)
            {
                _failureCount++;
                if (_failureCount >= MaxFailuresBeforeRecovery)
                {
                    _isServiceAvailable = false;
                    _logger.LogWarning("API externa ficou indisponível após {FailureCount} falhas", _failureCount);
                    
                    _ = Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(_ => 
                    {
                        _isServiceAvailable = true;
                        _failureCount = 0;
                        _logger.LogInformation("API externa recuperada e disponível novamente");
                    });
                }
                
                _logger.LogWarning("Falha temporária na API externa (simulada)");
                throw new Exception("Falha temporária na API externa");
            }

            // Simulate API processing time
            await Task.Delay(_random.Next(100, 501));

            if (!_isServiceAvailable)
            {
                _logger.LogWarning("Tentativa de acesso à API externa enquanto indisponível");
                throw new Exception("Serviço indisponível temporariamente");
            }

            // Validate minimum order quantity (1000 units)
            var totalQuantity = orderRequest.Items?.Sum(item => item.Quantity) ?? 0;
            if (totalQuantity < 1000)
            {
                _logger.LogWarning("Pedido rejeitado: quantidade total {TotalQuantity} é menor que o mínimo de 1000 unidades", totalQuantity);
                throw new OrderValidationException("A quantidade total do pedido deve ser de no mínimo 1000 unidades");
            }

            _failureCount = 0;

            // Simulate successful order processing
            var orderId = $"EXT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
            _logger.LogInformation("Pedido {OrderId} processado com sucesso. Total de itens: {ItemCount}, Quantidade total: {TotalQuantity}", 
                orderId, orderRequest.Items?.Count ?? 0, totalQuantity);

            return new ExternalOrderResponseDto
            {
                OrderId = orderId,
                Status = "RECEIVED",
                CreatedAt = DateTime.UtcNow,
                Items = orderRequest.Items.ConvertAll(item => new ExternalOrderItemResponseDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                })
            };
        }
    }
}
