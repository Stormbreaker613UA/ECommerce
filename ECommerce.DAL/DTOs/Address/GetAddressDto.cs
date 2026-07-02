using System;

namespace ECommerce.DAL.DTOs.Address
{
    public class GetAddressDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
