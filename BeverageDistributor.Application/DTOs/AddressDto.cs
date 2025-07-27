namespace BeverageDistributor.Application.DTOs
{
    public class AddressDto
    {
        public required string Street { get; set; }
        public required string Number { get; set; }
        public string? Complement { get; set; }
        public required string Neighborhood { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string PostalCode { get; set; }
        public bool IsMain { get; set; }
    }
}
