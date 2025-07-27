using FluentValidation;
using BeverageDistributor.Application.DTOs.Distributor;

namespace BeverageDistributor.Application.Validators
{
    public class CreateDistributorDtoValidator : AbstractValidator<CreateDistributorDto>
    {
        public CreateDistributorDtoValidator()
        {
            RuleFor(x => x.Cnpj)
                .NotEmpty().WithMessage("CNPJ é obrigatório")
                .Length(14).WithMessage("CNPJ deve conter 14 dígitos")
                .Matches("^[0-9]{14}$").WithMessage("CNPJ deve conter apenas números");

            RuleFor(x => x.CompanyName)
                .NotEmpty().WithMessage("Razão social é obrigatória")
                .Length(3, 200).WithMessage("Razão social deve ter entre 3 e 200 caracteres");

            RuleFor(x => x.TradingName)
                .NotEmpty().WithMessage("Nome fantasia é obrigatório")
                .Length(3, 200).WithMessage("Nome fantasia deve ter entre 3 e 200 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório")
                .EmailAddress().WithMessage("E-mail inválido");

            RuleForEach(x => x.PhoneNumbers).SetValidator(new PhoneNumberDtoValidator());
            RuleForEach(x => x.ContactNames).SetValidator(new ContactNameDtoValidator());
            RuleForEach(x => x.Addresses).SetValidator(new AddressDtoValidator());
        }
    }
}
