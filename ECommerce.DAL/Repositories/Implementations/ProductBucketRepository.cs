using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.ProductBucket;
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

    public async Task<ProductBucket?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductBuckets
            .Include(b => b.ProductBucketItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken);
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

    public async Task<bool> TryClearAsync(
        Guid bucketId,
        IReadOnlyCollection<ProductBucketItemCheckoutSnapshot> expectedItems,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedItems);

        if (expectedItems.Count == 0)
            throw new ArgumentOutOfRangeException(
                nameof(expectedItems),
                "Expected cart item snapshots must not be empty.");

        Dictionary<Guid, ProductBucketItemCheckoutSnapshot> expectedById;

        try
        {
            expectedById = expectedItems.ToDictionary(item => item.Id);
        }
        catch (ArgumentException)
        {
            return false;
        }

        // Validate the complete active snapshot, not only the rows that were
        // originally read. A committed quantity, price, product, deletion, or
        // newly-added-item change visible at this statement aborts checkout.
        // A later addition remains outside this order and is intentionally left
        // active for a subsequent checkout.
        var currentItems = await _dbContext.ProductBucketItems
            .Where(item => item.ProductBucketId == bucketId)
            .Select(item => new ProductBucketItemCheckoutSnapshot(
                item.Id,
                item.ProductId,
                item.Quantity,
                item.UnitPrice,
                item.UpdatedAt))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (currentItems.Count != expectedById.Count ||
            currentItems.Any(item =>
                !expectedById.TryGetValue(item.Id, out var expected) ||
                expected != item))
        {
            return false;
        }

        var deletedAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow;

        foreach (var expectedItem in expectedItems)
        {
            var affectedRows = await _dbContext.ProductBucketItems
                .Where(item =>
                    item.ProductBucketId == bucketId &&
                    item.Id == expectedItem.Id &&
                    item.ProductId == expectedItem.ProductId &&
                    item.Quantity == expectedItem.Quantity &&
                    item.UnitPrice == expectedItem.UnitPrice &&
                    item.UpdatedAt == expectedItem.UpdatedAt)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(item => item.IsDeleted, true)
                        .SetProperty(item => item.DeletedAt, deletedAt)
                        .SetProperty(item => item.UpdatedAt, updatedAt),
                    cancellationToken);

            if (affectedRows != 1)
                return false;
        }

        // Items added after the checkout snapshot are intentionally left in the
        // cart for a later checkout. They are not part of this order.
        return true;
    }
}
