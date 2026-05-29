namespace ECommerce.DAL.Entities;

public class ProductBucketItem : AuditableEntity
{
    public Guid ProductBucketId { get; set; }
    public virtual ProductBucket ProductBucket { get; set; } = null!;
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
