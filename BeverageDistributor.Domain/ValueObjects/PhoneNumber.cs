using System.Text.RegularExpressions;
using BeverageDistributor.Domain.Exceptions;

namespace BeverageDistributor.Domain.ValueObjects
{
    public class PhoneNumber
    {
        public string Number { get; private set; }
        public bool IsMain { get; private set; }

        protected PhoneNumber() { }

        public PhoneNumber(string number, bool isMain = false)
        {
            if (string.IsNullOrWhiteSpace(number))
                throw new DomainException("Phone number cannot be empty");

            // Basic phone number validation (can be enhanced)
            if (!Regex.IsMatch(number, @"^\+?[1-9]\d{1,14}$"))
                throw new DomainException("Invalid phone number format");

            Number = number;
            IsMain = isMain;
        }
    }
}
