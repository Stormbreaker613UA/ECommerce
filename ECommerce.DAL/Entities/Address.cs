namespace ECommerce.DAL.Entities;

public class Address : AuditableEntity
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public virtual User User { get; set; } = null!;
    public Guid UserId { get; set; }
}
