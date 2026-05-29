namespace ECommerce.DAL.Entities;

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
}
