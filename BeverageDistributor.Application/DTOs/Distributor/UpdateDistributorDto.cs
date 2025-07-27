using System.Collections.Generic;

namespace BeverageDistributor.Application.DTOs.Distributor
{
    public class UpdateDistributorDto
    {
        public string? Cnpj { get; set; }
        public string? CompanyName { get; set; }
        public string? TradingName { get; set; }
        public string? Email { get; set; }
        public List<PhoneNumberDto>? PhoneNumbers { get; set; }
        public List<ContactNameDto>? ContactNames { get; set; }
        public List<AddressDto>? Addresses { get; set; }
    }
}
