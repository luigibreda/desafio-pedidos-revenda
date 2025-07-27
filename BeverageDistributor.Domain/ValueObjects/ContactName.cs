using BeverageDistributor.Domain.Exceptions;

namespace BeverageDistributor.Domain.ValueObjects
{
    public class ContactName
    {
        public required string Name { get; set; }
        public bool IsPrimary { get; set; }

        public ContactName() { }

        public ContactName(string name, bool isPrimary = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Contact name cannot be empty");

            if (name.Length < 3 || name.Length > 100)
                throw new DomainException("Contact name must be between 3 and 100 characters");

            Name = name.Trim();
            IsPrimary = isPrimary;
        }
    }
}
