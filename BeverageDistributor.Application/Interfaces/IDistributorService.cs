using BeverageDistributor.Application.DTOs.Distributor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.Application.Interfaces
{
    public interface IDistributorService
    {
        Task<IEnumerable<DistributorResponseDto>> GetAllAsync();
        Task<DistributorResponseDto> GetByIdAsync(Guid id);
        Task<DistributorResponseDto> CreateAsync(CreateDistributorDto createDto);
        Task<DistributorResponseDto> UpdateAsync(Guid id, UpdateDistributorDto updateDto);
        Task<bool> DeleteAsync(Guid id);
    }
}
