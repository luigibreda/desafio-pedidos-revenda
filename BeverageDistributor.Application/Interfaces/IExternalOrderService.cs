using System.Threading.Tasks;
using BeverageDistributor.Application.DTOs.Integration;

namespace BeverageDistributor.Application.Interfaces
{
    public interface IExternalOrderService
    {
        Task<ExternalOrderResponseDto> SubmitOrderAsync(ExternalOrderRequestDto orderRequest);
    }
}
