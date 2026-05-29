namespace ECommerce.DAL.Entities;

public class Order : AuditableEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid OrderStatusId { get; set; }
    public virtual OrderStatus OrderStatus { get; set; } = new OrderStatus();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
