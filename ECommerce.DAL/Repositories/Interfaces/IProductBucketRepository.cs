using ECommerce.DAL.DTOs.ProductBucket;
using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IProductBucketRepository
{
    Task<ProductBucket?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ProductBucket?> GetByIdAsync(Guid bucketId);
    Task<ProductBucketItem?> GetItemAsync(Guid bucketId, Guid productId);
    Task AddAsync(ProductBucket bucket);
    Task UpdateAsync(ProductBucket bucket);
    Task AddItemAsync(ProductBucketItem item);
    Task RemoveItemAsync(ProductBucketItem item);
    Task ClearAsync(Guid bucketId);

    Task<bool> TryClearAsync(
        Guid bucketId,
        IReadOnlyCollection<ProductBucketItemCheckoutSnapshot> expectedItems,
        CancellationToken cancellationToken = default);
}
