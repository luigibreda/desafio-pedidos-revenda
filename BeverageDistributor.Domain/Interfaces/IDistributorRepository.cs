using BeverageDistributor.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.Domain.Interfaces
{
    public interface IDistributorRepository
    {
        Task<Distributor> GetByIdAsync(Guid id);
        Task<IEnumerable<Distributor>> GetAllAsync();
        Task<Distributor> AddAsync(Distributor distributor);
        Task UpdateAsync(Distributor distributor);
        Task DeleteAsync(Guid id);
    }
}
