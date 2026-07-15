namespace ECommerce.DAL.DTOs.ProductBucket;

public class GetProductBucketDto
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public List<GetProductBucketItemDto> Items { get; set; } = new();

    public decimal TotalPrice { get; set; }
}
