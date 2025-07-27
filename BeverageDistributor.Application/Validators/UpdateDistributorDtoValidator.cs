using BeverageDistributor.Application.DTOs.Distributor;
using BeverageDistributor.Application.DTOs;
using FluentValidation;
using System.Linq;
using System.Collections.Generic;
using BeverageDistributor.Domain.ValueObjects;

namespace BeverageDistributor.Application.Validators
{
    public class UpdateDistributorDtoValidator : AbstractValidator<UpdateDistributorDto>
    {
        public UpdateDistributorDtoValidator()
        {
            RuleFor(x => x.Cnpj)
                .NotEmpty().When(x => x.Cnpj != null)
                .WithMessage("CNPJ não pode estar vazio")
                .Length(14).When(x => !string.IsNullOrEmpty(x.Cnpj))
                .WithMessage("CNPJ deve ter 14 caracteres")
                .Must(BeAValidCnpj).When(x => !string.IsNullOrEmpty(x.Cnpj))
                .WithMessage("CNPJ inválido");

            RuleFor(x => x.CompanyName)
                .NotEmpty().When(x => x.CompanyName != null)
                .WithMessage("Razão Social não pode estar vazia")
                .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.CompanyName))
                .WithMessage("Razão Social não pode ter mais de 200 caracteres");

            RuleFor(x => x.TradingName)
                .NotEmpty().When(x => x.TradingName != null)
                .WithMessage("Nome Fantasia não pode estar vazio")
                .MaximumLength(200).When(x => !string.IsNullOrEmpty(x.TradingName))
                .WithMessage("Nome Fantasia não pode ter mais de 200 caracteres");

            RuleFor(x => x.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("E-mail inválido");

            RuleForEach(x => x.PhoneNumbers)
                .SetValidator(new PhoneNumberDtoValidator())
                .When(x => x.PhoneNumbers != null);

            RuleFor(x => x.ContactNames)
                .Must(HaveOnePrimaryContact).When(x => x.ContactNames != null && x.ContactNames.Any())
                .WithMessage("Deve haver exatamente um contato principal");

            RuleForEach(x => x.ContactNames)
                .SetValidator(new ContactNameDtoValidator())
                .When(x => x.ContactNames != null);

            RuleFor(x => x.Addresses)
                .Must(HaveOneMainAddress).When(x => x.Addresses != null && x.Addresses.Any())
                .WithMessage("Deve haver exatamente um endereço principal");

            RuleForEach(x => x.Addresses)
                .SetValidator(new AddressDtoValidator())
                .When(x => x.Addresses != null);
        }

        private bool BeAValidCnpj(string cnpj)
        {
            // Remove caracteres não numéricos
            cnpj = new string(cnpj.Where(char.IsDigit).ToArray());

            // Verifica se tem 14 dígitos
            if (cnpj.Length != 14)
                return false;

            // Verifica se todos os dígitos são iguais
            if (cnpj.All(c => c == cnpj[0]))
                return false;

            // Validação do primeiro dígito verificador
            int[] multiplicadores1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int soma = 0;
            for (int i = 0; i < 12; i++)
                soma += int.Parse(cnpj[i].ToString()) * multiplicadores1[i];
            int resto = soma % 11;
            int digito1 = resto < 2 ? 0 : 11 - resto;

            if (digito1 != int.Parse(cnpj[12].ToString()))
                return false;

            // Validação do segundo dígito verificador
            int[] multiplicadores2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            soma = 0;
            for (int i = 0; i < 13; i++)
                soma += int.Parse(cnpj[i].ToString()) * multiplicadores2[i];
            resto = soma % 11;
            int digito2 = resto < 2 ? 0 : 11 - resto;

            return digito2 == int.Parse(cnpj[13].ToString());
        }

        private bool HaveOnePrimaryContact(ICollection<ContactNameDto> contacts)
        {
            if (contacts == null || !contacts.Any())
                return true;

            return contacts.Count(c => c.IsPrimary) == 1;
        }

        private bool HaveOneMainAddress(ICollection<AddressDto> addresses)
        {
            if (addresses == null || !addresses.Any())
                return true;

            return addresses.Count(a => a.IsMain) == 1;
        }
    }
}
