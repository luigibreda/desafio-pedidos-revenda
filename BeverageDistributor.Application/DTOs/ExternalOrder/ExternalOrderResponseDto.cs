using System;
using System.Collections.Generic;

namespace BeverageDistributor.Application.Dtos.ExternalOrder
{
    public class ExternalOrderResponseDto
    {
        public string ExternalOrderId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<ExternalOrderItemResponseDto> Items { get; set; } = new();
    }

    public class ExternalOrderItemResponseDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
