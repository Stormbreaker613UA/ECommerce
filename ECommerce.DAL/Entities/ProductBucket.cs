namespace ECommerce.DAL.Entities;

public class ProductBucket : AuditableEntity
{  
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual ICollection<ProductBucketItem> ProductBucketItems { get; set; } = new List<ProductBucketItem>();
}
