using FluentValidation;
using BeverageDistributor.Application.DTOs;

namespace BeverageDistributor.Application.Validators
{
    public class PhoneNumberDtoValidator : AbstractValidator<PhoneNumberDto>
    {
        public PhoneNumberDtoValidator()
        {
            RuleFor(x => x.Number)
                .NotEmpty().WithMessage("Número de telefone é obrigatório")
                .Matches("^[0-9]+").WithMessage("Número de telefone inválido")
                .Length(10, 20).WithMessage("Número de telefone deve ter entre 10 e 20 dígitos");
        }
    }
}
