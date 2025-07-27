using System.Collections.Generic;

namespace BeverageDistributor.Application.DTOs.Integration
{
    public class ExternalOrderRequestDto
    {
        public string DistributorId { get; set; }
        public List<ExternalOrderItemDto> Items { get; set; } = new List<ExternalOrderItemDto>();
    }

    public class ExternalOrderItemDto
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
