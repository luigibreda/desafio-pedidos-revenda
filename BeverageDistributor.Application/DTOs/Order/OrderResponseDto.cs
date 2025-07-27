using System;
using System.Collections.Generic;

namespace BeverageDistributor.Application.DTOs.Order
{
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public Guid DistributorId { get; set; }
        public string DistributorName { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
