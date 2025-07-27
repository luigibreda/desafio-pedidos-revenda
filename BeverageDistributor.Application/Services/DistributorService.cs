using AutoMapper;
using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Interfaces;
using BeverageDistributor.Domain.ValueObjects;
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
            var distributor = new Distributor
            {
                Cnpj = createDto.Cnpj,
                CompanyName = createDto.CompanyName,
                TradingName = createDto.TradingName,
                Email = createDto.Email
            };

            // Mapear telefones, contatos e endereços
            if (createDto.PhoneNumbers != null)
            {
                foreach (var phoneDto in createDto.PhoneNumbers)
                {
                    distributor.AddPhoneNumber(new PhoneNumber 
                    { 
                        Number = phoneDto.Number, 
                        IsMain = phoneDto.IsMain 
                    });
                }
            }

            if (createDto.ContactNames != null)
            {
                foreach (var contactDto in createDto.ContactNames)
                {
                    distributor.AddContactName(new ContactName 
                    { 
                        Name = contactDto.Name, 
                        IsPrimary = contactDto.IsPrimary 
                    });
                }
            }

            if (createDto.Addresses != null)
            {
                foreach (var addressDto in createDto.Addresses)
                {
                    distributor.AddAddress(new Address
                    {
                        Street = addressDto.Street,
                        Number = addressDto.Number,
                        Complement = addressDto.Complement,
                        Neighborhood = addressDto.Neighborhood,
                        City = addressDto.City,
                        State = addressDto.State,
                        Country = "Brasil", // Definindo Brasil como valor padrão
                        PostalCode = addressDto.PostalCode,
                        IsMain = addressDto.IsMain
                    });
                }
            }
            
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
