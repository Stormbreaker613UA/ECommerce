namespace ECommerce.DAL.Entities;

public class OrderItem : AuditableEntity
{
    public virtual Order Order { get; set; } = null!;
    public virtual Guid OrderId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public virtual Guid ProductId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

}
