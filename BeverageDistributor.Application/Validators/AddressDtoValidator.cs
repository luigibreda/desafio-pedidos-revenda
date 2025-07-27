using FluentValidation;
using BeverageDistributor.Application.DTOs;

namespace BeverageDistributor.Application.Validators
{
    public class AddressDtoValidator : AbstractValidator<AddressDto>
    {
        public AddressDtoValidator()
        {
            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Logradouro é obrigatório")
                .Length(3, 200).WithMessage("Logradouro deve ter entre 3 e 200 caracteres");

            RuleFor(x => x.Number)
                .NotEmpty().WithMessage("Número é obrigatório")
                .Length(1, 20).WithMessage("Número deve ter entre 1 e 20 caracteres");

            RuleFor(x => x.Neighborhood)
                .NotEmpty().WithMessage("Bairro é obrigatório")
                .Length(3, 100).WithMessage("Bairro deve ter entre 3 e 100 caracteres");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("Cidade é obrigatória")
                .Length(3, 100).WithMessage("Cidade deve ter entre 3 e 100 caracteres");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("Estado é obrigatório")
                .Length(2).WithMessage("Estado deve ter 2 caracteres")
                .Matches("^[A-Z]{2}$").WithMessage("Estado deve ser a sigla com 2 letras maiúsculas");

            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("CEP é obrigatório")
                .Length(8).WithMessage("CEP deve conter 8 dígitos")
                .Matches("^[0-9]{8}$").WithMessage("CEP deve conter apenas números");
        }
    }
}
