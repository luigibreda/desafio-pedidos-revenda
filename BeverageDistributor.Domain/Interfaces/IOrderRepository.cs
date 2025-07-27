using BeverageDistributor.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.Domain.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetByDistributorIdAsync(Guid distributorId);
        Task<IEnumerable<Order>> GetByClientIdAsync(string clientId);
        Task<Order> AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}
