using System;
using System.Collections.Generic;

namespace BeverageDistributor.Application.Dtos.ExternalOrder
{
    public class ExternalOrderRequestDto
    {
        public Guid DistributorId { get; set; }
        public List<ExternalOrderItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class ExternalOrderItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
