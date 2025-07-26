using BeverageDistributor.Domain.Exceptions;

namespace BeverageDistributor.Domain.ValueObjects
{
    public class Address
    {
        public string Street { get; private set; }
        public string Number { get; private set; }
        public string Complement { get; private set; }
        public string Neighborhood { get; private set; }
        public string City { get; private set; }
        public string State { get; private set; }
        public string Country { get; private set; }
        public string PostalCode { get; private set; }
        public bool IsMain { get; private set; }

        protected Address() { }

        public Address(string street, string number, string neighborhood, string city, 
                      string state, string country, string postalCode, string complement = "", bool isMain = false)
        {
            if (string.IsNullOrWhiteSpace(street)) throw new DomainException("Street is required");
            if (string.IsNullOrWhiteSpace(number)) throw new DomainException("Number is required");
            if (string.IsNullOrWhiteSpace(neighborhood)) throw new DomainException("Neighborhood is required");
            if (string.IsNullOrWhiteSpace(city)) throw new DomainException("City is required");
            if (string.IsNullOrWhiteSpace(state)) throw new DomainException("State is required");
            if (string.IsNullOrWhiteSpace(country)) throw new DomainException("Country is required");
            if (string.IsNullOrWhiteSpace(postalCode)) throw new DomainException("Postal code is required");

            Street = street.Trim();
            Number = number.Trim();
            Neighborhood = neighborhood.Trim();
            City = city.Trim();
            State = state.Trim();
            Country = country.Trim();
            PostalCode = postalCode.Trim().Replace("-", "").Replace(".", "").Trim();
            Complement = complement?.Trim() ?? string.Empty;
            IsMain = isMain;
        }
    }
}
