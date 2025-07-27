using FluentValidation;
using BeverageDistributor.Application.DTOs;

namespace BeverageDistributor.Application.Validators
{
    public class ContactNameDtoValidator : AbstractValidator<ContactNameDto>
    {
        public ContactNameDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nome do contato é obrigatório")
                .Length(3, 100).WithMessage("Nome do contato deve ter entre 3 e 100 caracteres");
        }
    }
}
