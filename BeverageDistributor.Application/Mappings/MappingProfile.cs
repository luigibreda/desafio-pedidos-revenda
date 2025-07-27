using AutoMapper;
using BeverageDistributor.Application.DTOs;
using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.ValueObjects;

namespace BeverageDistributor.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Distributor, DistributorResponseDto>();
            
            CreateMap<CreateDistributorDto, Distributor>()
                .ForMember(dest => dest.PhoneNumbers, opt => opt.Ignore())
                .ForMember(dest => dest.ContactNames, opt => opt.Ignore())
                .ForMember(dest => dest.Addresses, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    foreach (var phoneDto in src.PhoneNumbers)
                    {
                        dest.AddPhoneNumber(new PhoneNumber(phoneDto.Number, phoneDto.IsMain));
                    }

                    foreach (var contactDto in src.ContactNames)
                    {
                        dest.AddContactName(new ContactName(contactDto.Name, contactDto.IsPrimary));
                    }

                    foreach (var addressDto in src.Addresses)
                    {
                        dest.AddAddress(new Address(
                            street: addressDto.Street,
                            number: addressDto.Number,
                            neighborhood: addressDto.Neighborhood,
                            city: addressDto.City,
                            state: addressDto.State,
                            country: "Brasil",
                            postalCode: addressDto.PostalCode,
                            complement: addressDto.Complement ?? string.Empty,
                            isMain: addressDto.IsMain));
                    }
                });
        }
    }
}
