using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeverageDistributor.Application.DTOs.Integration;
using BeverageDistributor.Application.Interfaces;
using Microsoft.Extensions.Logging;

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
                
                throw new Exception("Falha temporária na API externa");
            }

            await Task.Delay(_random.Next(100, 501));

            if (!_isServiceAvailable)
            {
                throw new Exception("Serviço indisponível temporariamente");
            }

            _failureCount = 0;

            return new ExternalOrderResponseDto
            {
                OrderId = $"EXT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
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
