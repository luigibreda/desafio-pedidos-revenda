using System.ComponentModel.DataAnnotations;

namespace BeverageDistributor.Application.DTOs.Order
{
    public class UpdateOrderStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = string.Empty;
    }
}
