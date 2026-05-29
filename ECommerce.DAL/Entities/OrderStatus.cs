namespace ECommerce.DAL.Entities;

public class OrderStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Business rules
    public bool CanCancel { get; set; } = false;
    public bool CanRefund { get; set; } = false;
    public bool CanShip { get; set; } = false;

    public bool IsActive { get; set; } = true;

    // Navigation

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
