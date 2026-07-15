namespace ECommerce.DAL.DTOs.ProductBucket;

public class AddProductToBucketDto
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}