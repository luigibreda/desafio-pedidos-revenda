using System.Collections.Generic;

namespace BeverageDistributor.Application.DTOs.Distributor
{
    public class CreateDistributorDto
    {
        public required string Cnpj { get; set; }
        public required string CompanyName { get; set; }
        public required string TradingName { get; set; }
        public required string Email { get; set; }
        public List<PhoneNumberDto> PhoneNumbers { get; set; } = new();
        public List<ContactNameDto> ContactNames { get; set; } = new();
        public List<AddressDto> Addresses { get; set; } = new();
    }
}
