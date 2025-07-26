using BeverageDistributor.Domain.Exceptions;

namespace BeverageDistributor.Domain.ValueObjects
{
    public class ContactName
    {
        public string Name { get; private set; }
        public bool IsPrimary { get; private set; }

        protected ContactName() { }

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
