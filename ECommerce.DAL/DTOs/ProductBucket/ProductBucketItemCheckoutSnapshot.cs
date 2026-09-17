namespace ECommerce.DAL.DTOs.ProductBucket;

public sealed record ProductBucketItemCheckoutSnapshot(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    DateTime? UpdatedAt);
