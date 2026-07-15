namespace ECommerce.DAL.DTOs.ProductBucket;

public class GetProductBucketItemDto
{
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal TotalPrice { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}