using AutoMapper;
using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeverageDistributor.Application.Services
{
    public class DistributorService : IDistributorService
    {
        private readonly IDistributorRepository _distributorRepository;
        private readonly IMapper _mapper;

        public DistributorService(IDistributorRepository distributorRepository, IMapper mapper)
        {
            _distributorRepository = distributorRepository;
            _mapper = mapper;
        }

        public async Task<DistributorResponseDto> CreateAsync(CreateDistributorDto createDto)
        {
            var distributor = new Distributor(
                createDto.Cnpj,
                createDto.CompanyName,
                createDto.TradingName,
                createDto.Email);

            // Mapear telefones, contatos e endereços
            // (implementação simplificada - pode ser melhorada)
            
            var created = await _distributorRepository.AddAsync(distributor);
            return _mapper.Map<DistributorResponseDto>(created);
        }

        public async Task<IEnumerable<DistributorResponseDto>> GetAllAsync()
        {
            var distributors = await _distributorRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<DistributorResponseDto>>(distributors);
        }

        public async Task<DistributorResponseDto> GetByIdAsync(Guid id)
        {
            var distributor = await _distributorRepository.GetByIdAsync(id);
            return _mapper.Map<DistributorResponseDto>(distributor);
        }
    }
}
