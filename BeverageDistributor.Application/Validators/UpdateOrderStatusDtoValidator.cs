using BeverageDistributor.Application.DTOs.Order;
using FluentValidation;

namespace BeverageDistributor.Application.Validators
{
    public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
    {
        public UpdateOrderStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status é obrigatório")
                .Must(BeAValidStatus).WithMessage("Status inválido. Valores válidos: 'processing', 'completed', 'cancelled'");
        }

        private bool BeAValidStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.ToLower() == "processing" || 
                   status.ToLower() == "completed" || 
                   status.ToLower() == "cancelled";
        }
    }
}
