using AutoMapper;
using BeverageDistributor.Application.DTOs;
using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Application.DTOs.Order;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Enums;
using BeverageDistributor.Domain.ValueObjects;

namespace BeverageDistributor.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Distributor, DistributorResponseDto>()
                .ForMember(dest => dest.PhoneNumbers, opt => opt.MapFrom(src => src.PhoneNumbers))
                .ForMember(dest => dest.ContactNames, opt => opt.MapFrom(src => src.ContactNames))
                .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.Addresses));
            
            // Mapeamentos para os DTOs de valor
            CreateMap<PhoneNumber, PhoneNumberDto>();
            CreateMap<ContactName, ContactNameDto>();
            CreateMap<Address, AddressDto>();
            
            // Mapeamentos para pedidos
            CreateMap<Order, OrderResponseDto>();
            CreateMap<OrderItem, OrderItemDto>();
            
            CreateMap<CreateOrderDto, Order>()
                .ForMember(dest => dest.Items, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    foreach (var itemDto in src.Items)
                    {
                        dest.AddItem(
                            itemDto.ProductId,
                            itemDto.ProductName,
                            itemDto.Quantity,
                            itemDto.UnitPrice);
                    }
                });
            
            CreateMap<CreateDistributorDto, Distributor>()
                .ForMember(dest => dest.PhoneNumbers, opt => opt.Ignore())
                .ForMember(dest => dest.ContactNames, opt => opt.Ignore())
                .ForMember(dest => dest.Addresses, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    if (src.PhoneNumbers != null)
                    {
                        foreach (var phoneDto in src.PhoneNumbers)
                        {
                            dest.AddPhoneNumber(new PhoneNumber 
                            { 
                                Number = phoneDto.Number, 
                                IsMain = phoneDto.IsMain 
                            });
                        }
                    }

                    if (src.ContactNames != null)
                    {
                        foreach (var contactDto in src.ContactNames)
                        {
                            dest.AddContactName(new ContactName 
                            { 
                                Name = contactDto.Name, 
                                IsPrimary = contactDto.IsPrimary 
                            });
                        }
                    }

                    if (src.Addresses != null)
                    {
                        foreach (var addressDto in src.Addresses)
                        {
                            dest.AddAddress(new Address
                            {
                                Street = addressDto.Street,
                                Number = addressDto.Number,
                                Neighborhood = addressDto.Neighborhood,
                                City = addressDto.City,
                                State = addressDto.State,
                                Country = "Brasil", // Valor padrão para o Brasil
                                PostalCode = addressDto.PostalCode,
                                Complement = addressDto.Complement,
                                IsMain = addressDto.IsMain
                            });
                        }
                    }
                });
        }
    }
}
