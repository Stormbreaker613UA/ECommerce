using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IProductBucketRepository
{
    Task<ProductBucket?> GetByUserIdAsync(Guid userId);
    Task<ProductBucket?> GetByIdAsync(Guid bucketId);
    Task<ProductBucketItem?> GetItemAsync(Guid bucketId, Guid productId);
    Task AddAsync(ProductBucket bucket);
    Task UpdateAsync(ProductBucket bucket);
    Task AddItemAsync(ProductBucketItem item);
    Task RemoveItemAsync(ProductBucketItem item);
    Task ClearAsync(Guid bucketId);
}