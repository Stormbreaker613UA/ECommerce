using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IProductBucketRepository
{
    Task<ProductBucket?> GetByUserIdAsync(Guid userId);
    Task<ProductBucket?> GetByIdAsync(Guid bucketId);

    Task AddAsync(ProductBucket bucket);
    Task UpdateAsync(ProductBucket bucket);
}
