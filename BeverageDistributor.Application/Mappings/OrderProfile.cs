using AutoMapper;
using BeverageDistributor.Application.DTOs.Integration;
using BeverageDistributor.Application.DTOs.Order;
using BeverageDistributor.Domain.Entities;
using BeverageDistributor.Domain.Enums;

namespace BeverageDistributor.Application.Mappings
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            // Mapeamento de Order para OrderResponseDto
            CreateMap<Order, OrderResponseDto>()
                .ForMember(dest => dest.DistributorName, opt => opt.MapFrom(src => src.Distributor.CompanyName))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
                // TotalAmount é mapeado automaticamente da propriedade TotalAmount da entidade

            // Mapeamento de CreateOrderDto para Order
            CreateMap<CreateOrderDto, Order>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => OrderStatus.Pending))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // Mapeamento de OrderItem para OrderItemDto
            CreateMap<OrderItem, OrderItemDto>();

            // Mapeamento de OrderItemDto para OrderItem
            CreateMap<OrderItemDto, OrderItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.OrderId, opt => opt.Ignore());

            // Mapeamento para integração com API externa
            CreateMap<Order, ExternalOrderRequestDto>()
                .ForMember(dest => dest.DistributorId, opt => opt.MapFrom(src => src.DistributorId.ToString()))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.Select(item => new ExternalOrderItemDto
                {
                    ProductId = item.ProductId.ToString(),
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                })));
        }
    }
}
