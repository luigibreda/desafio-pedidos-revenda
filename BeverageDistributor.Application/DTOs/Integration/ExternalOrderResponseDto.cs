using System;
using System.Collections.Generic;

namespace BeverageDistributor.Application.DTOs.Integration
{
    public class ExternalOrderResponseDto
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExternalOrderItemResponseDto> Items { get; set; } = new List<ExternalOrderItemResponseDto>();
    }

    public class ExternalOrderItemResponseDto
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
