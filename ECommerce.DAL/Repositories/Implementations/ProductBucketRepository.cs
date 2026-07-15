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
                    .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<ProductBucket?> GetByIdAsync(Guid bucketId)
    {
        return await _dbContext.ProductBuckets
            .Include(b => b.ProductBucketItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(b => b.Id == bucketId);
    }

    public async Task<ProductBucketItem?> GetItemAsync(Guid bucketId, Guid productId)
    {
        return await _dbContext.ProductBucketItems
            .Include(i => i.Product)
                .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(i =>
                i.ProductBucketId == bucketId &&
                i.ProductId == productId);
    }

    public async Task AddAsync(ProductBucket bucket)
    {
        await _dbContext.ProductBuckets.AddAsync(bucket);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductBucket bucket)
    {
        _dbContext.ProductBuckets.Update(bucket);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddItemAsync(ProductBucketItem item)
    {
        await _dbContext.ProductBucketItems.AddAsync(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveItemAsync(ProductBucketItem item)
    {
        _dbContext.ProductBucketItems.Remove(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ClearAsync(Guid bucketId)
    {
        var bucket = await _dbContext.ProductBuckets
            .Include(b => b.ProductBucketItems)
            .FirstOrDefaultAsync(b => b.Id == bucketId);

        if (bucket == null)
            throw new KeyNotFoundException("Shopping cart not found.");

        _dbContext.ProductBucketItems.RemoveRange(bucket.ProductBucketItems);

        await _dbContext.SaveChangesAsync();
    }
}