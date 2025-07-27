using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BeverageDistributor.Application.DTOs.Order
{
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Distributor ID is required")]
        public Guid DistributorId { get; set; }

        [Required(ErrorMessage = "Client ID is required")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one order item is required")]
        [MinLength(1, ErrorMessage = "At least one order item is required")]
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
