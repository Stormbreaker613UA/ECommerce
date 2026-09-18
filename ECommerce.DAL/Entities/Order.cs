namespace ECommerce.DAL.Entities;

public class Order : AuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    // The address selected at checkout is retained as historical order data.
    // It intentionally is not a live navigation: Address may later be edited
    // or soft-deleted, while an invoice must preserve what was selected.
    public Guid? BillingAddressId { get; set; }
    public string? BillingAddressSnapshot { get; set; }
    public Guid OrderStatusId { get; set; }
    public virtual OrderStatus OrderStatus { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
