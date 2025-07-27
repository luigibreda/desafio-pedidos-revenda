using System.Collections.Generic;
using System.Linq;
using BeverageDistributor.Domain.Exceptions;
using BeverageDistributor.Domain.ValueObjects;

namespace BeverageDistributor.Domain.Entities
{
    public class Distributor : BaseEntity
    {
        public required string Cnpj { get; set; }
        public required string CompanyName { get; set; }
        public required string TradingName { get; set; }
        public required string Email { get; set; }
        private readonly List<PhoneNumber> _phoneNumbers = new();
        private readonly List<ContactName> _contactNames = new();
        private readonly List<Address> _addresses = new();

        public Distributor() { }

        public Distributor(string cnpj, string companyName, string tradingName, string email)
        {
            SetCnpj(cnpj);
            SetCompanyName(companyName);
            SetTradingName(tradingName);
            SetEmail(email);
        }

        public IReadOnlyCollection<PhoneNumber> PhoneNumbers => _phoneNumbers.AsReadOnly();
        public IReadOnlyCollection<ContactName> ContactNames => _contactNames.AsReadOnly();
        public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

        public void SetCnpj(string cnpj)
        {
            if (string.IsNullOrWhiteSpace(cnpj))
                throw new DomainException("CNPJ é obrigatório");

            var cleanCnpj = cnpj.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
            
            if (cleanCnpj.Length != 14 || !long.TryParse(cleanCnpj, out _))
                throw new DomainException("Formato de CNPJ inválido");

            Cnpj = cleanCnpj;
        }

        public void SetCompanyName(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName) || companyName.Length < 3 || companyName.Length > 200)
                throw new DomainException("A razão social deve ter entre 3 e 200 caracteres");

            CompanyName = companyName.Trim();
        }

        public void SetTradingName(string tradingName)
        {
            if (string.IsNullOrWhiteSpace(tradingName) || tradingName.Length < 3 || tradingName.Length > 200)
                throw new DomainException("O nome fantasia deve ter entre 3 e 200 caracteres");

            TradingName = tradingName.Trim();
        }

        public void SetEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new DomainException("E-mail é obrigatório");

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email.Trim())
                    throw new DomainException("Formato de e-mail inválido");
            }
            catch
            {
                throw new DomainException("Formato de e-mail inválido");
            }

            Email = email.Trim();
        }

        public void AddPhoneNumber(PhoneNumber phoneNumber)
        {
            _phoneNumbers.Add(phoneNumber);
        }

        public void AddContactName(ContactName contactName)
        {
            if (contactName.IsPrimary && _contactNames.Any(c => c.IsPrimary))
            {
                var currentPrimary = _contactNames.First(c => c.IsPrimary);
                _contactNames.Remove(currentPrimary);
                _contactNames.Add(new ContactName { Name = currentPrimary.Name, IsPrimary = false });
            }
            _contactNames.Add(contactName);
        }

        public void AddAddress(Address address)
        {
            if (address.IsMain && _addresses.Any(a => a.IsMain))
            {
                var currentMain = _addresses.First(a => a.IsMain);
                _addresses.Remove(currentMain);
                var newAddress = new Address
                {
                    Street = currentMain.Street,
                    Number = currentMain.Number,
                    Neighborhood = currentMain.Neighborhood,
                    City = currentMain.City,
                    State = currentMain.State,
                    Country = currentMain.Country,
                    PostalCode = currentMain.PostalCode,
                    Complement = currentMain.Complement,
                    IsMain = false
                };
                _addresses.Add(newAddress);
            }
            _addresses.Add(address);
        }

        public void UpdatePhoneNumbers(IEnumerable<PhoneNumber> phoneNumbers)
        {
            _phoneNumbers.Clear();
            foreach (var phone in phoneNumbers)
            {
                _phoneNumbers.Add(phone);
            }
        }

        public void UpdateContactNames(IEnumerable<ContactName> contactNames)
        {
            _contactNames.Clear();
            foreach (var contact in contactNames)
            {
                _contactNames.Add(contact);
            }
        }

        public void UpdateAddresses(IEnumerable<Address> addresses)
        {
            _addresses.Clear();
            foreach (var address in addresses)
            {
                _addresses.Add(address);
            }
        }
    }
}
