namespace BeverageDistributor.Application.DTOs.Distributor
{
    public class DistributorResponseDto
    {
        public required Guid Id { get; set; }
        public required string Cnpj { get; set; }
        public required string CompanyName { get; set; }
        public required string TradingName { get; set; }
        public required string Email { get; set; }
        public ICollection<PhoneNumberDto> PhoneNumbers { get; set; } = new List<PhoneNumberDto>();
        public ICollection<ContactNameDto> ContactNames { get; set; } = new List<ContactNameDto>();
        public ICollection<AddressDto> Addresses { get; set; } = new List<AddressDto>();
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
