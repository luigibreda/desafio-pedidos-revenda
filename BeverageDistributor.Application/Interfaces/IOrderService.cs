using BeverageDistributor.Application.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> GetByIdAsync(Guid id);
        Task<IEnumerable<OrderResponseDto>> GetByDistributorIdAsync(Guid distributorId);
        Task<IEnumerable<OrderResponseDto>> GetByClientIdAsync(string clientId);
        Task<OrderResponseDto> CreateAsync(CreateOrderDto createDto);
        Task<OrderResponseDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto updateDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
