using AutoMapper;
using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Application.Interfaces;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Interfaces;
using BeverageDistributor.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
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
            // Check if a distributor with the same CNPJ already exists
            var existingDistributor = await _distributorRepository.GetByCnpjAsync(createDto.Cnpj);
            if (existingDistributor != null)
            {
                throw new InvalidOperationException($"A distributor with CNPJ {createDto.Cnpj} already exists.");
            }

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
            if (distributor == null)
                throw new KeyNotFoundException($"Distributor with ID {id} not found");
                
            return _mapper.Map<DistributorResponseDto>(distributor);
        }

        public async Task<DistributorResponseDto> UpdateAsync(Guid id, UpdateDistributorDto updateDto)
        {
            var distributor = await _distributorRepository.GetByIdAsync(id);
            if (distributor == null)
                throw new KeyNotFoundException($"Distributor with ID {id} not found");

            // Atualiza apenas os campos fornecidos
            if (updateDto.Cnpj != null && updateDto.Cnpj != distributor.Cnpj)
            {
                // Verifica se o novo CNPJ já existe
                var existingDistributor = await _distributorRepository.GetByCnpjAsync(updateDto.Cnpj);
                if (existingDistributor != null && existingDistributor.Id != id)
                    throw new InvalidOperationException($"A distributor with CNPJ {updateDto.Cnpj} already exists.");
                
                distributor.SetCnpj(updateDto.Cnpj);
            }

            if (updateDto.CompanyName != null)
                distributor.SetCompanyName(updateDto.CompanyName);

            if (updateDto.TradingName != null)
                distributor.SetTradingName(updateDto.TradingName);

            if (updateDto.Email != null)
                distributor.SetEmail(updateDto.Email);

            // Atualiza telefones, se fornecidos
            if (updateDto.PhoneNumbers != null)
            {
                // Cria uma nova coleção de telefones
                var phoneNumbers = new List<PhoneNumber>();
                foreach (var phoneDto in updateDto.PhoneNumbers)
                {
                    phoneNumbers.Add(new PhoneNumber 
                    { 
                        Number = phoneDto.Number, 
                        IsMain = phoneDto.IsMain 
                    });
                }
                // Atualiza a coleção de telefones do distribuidor
                distributor.UpdatePhoneNumbers(phoneNumbers);
            }

            // Atualiza contatos, se fornecidos
            if (updateDto.ContactNames != null)
            {
                // Cria uma nova coleção de contatos
                var contactNames = new List<ContactName>();
                foreach (var contactDto in updateDto.ContactNames)
                {
                    contactNames.Add(new ContactName 
                    { 
                        Name = contactDto.Name, 
                        IsPrimary = contactDto.IsPrimary 
                    });
                }
                // Atualiza a coleção de contatos do distribuidor
                distributor.UpdateContactNames(contactNames);
            }

            // Atualiza endereços, se fornecidos
            if (updateDto.Addresses != null)
            {
                // Cria uma nova coleção de endereços
                var addresses = new List<Address>();
                foreach (var addressDto in updateDto.Addresses)
                {
                    addresses.Add(new Address
                    {
                        Street = addressDto.Street,
                        Number = addressDto.Number,
                        Neighborhood = addressDto.Neighborhood,
                        City = addressDto.City,
                        State = addressDto.State,
                        Country = "Brasil",
                        PostalCode = addressDto.PostalCode,
                        Complement = addressDto.Complement ?? string.Empty,
                        IsMain = addressDto.IsMain
                    });
                }
                // Atualiza a coleção de endereços do distribuidor
                distributor.UpdateAddresses(addresses);
            }

            await _distributorRepository.UpdateAsync(distributor);
            return _mapper.Map<DistributorResponseDto>(distributor);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var distributor = await _distributorRepository.GetByIdAsync(id);
            if (distributor == null)
                return false;

            await _distributorRepository.DeleteAsync(id);
            return true;
        }
    }
}
