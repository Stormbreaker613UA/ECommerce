using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class ProductBucketRepository : IProductBucketRepository
{
    private readonly ECommerceDbContext _dbContext;

    public ProductBucketRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductBucket?> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.ProductBuckets
            .Include(b => b.ProductBucketItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<ProductBucket?> GetByIdAsync(Guid bucketId)
    {
        return await _dbContext.ProductBuckets
            .Include(b => b.ProductBucketItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(b => b.Id == bucketId);
    }

    public async Task AddAsync(ProductBucket bucket)
    {
        await _dbContext.ProductBuckets.AddAsync(bucket);
    }

    public Task UpdateAsync(ProductBucket bucket)
    {
        _dbContext.ProductBuckets.Update(bucket);
        return Task.CompletedTask;
    }
}