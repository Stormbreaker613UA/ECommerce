using ECommerce.BLL.Services.Implementations;
using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.Order;
using ECommerce.DAL.DTOs.ProductBucket;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Implementations;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.OrderTests;

public static class OrderServiceFactory
{
    public static OrderService Create(
        ECommerceDbContext context,
        IProductBucketRepository? productBucketRepository = null)
    {
        return new OrderService(
            context,
            new OrderRepository(context),
            new UserRepository(context),
            new AddressRepository(context),
            new ProductRepository(context),
            productBucketRepository ?? new ProductBucketRepository(context),
            new OrderStatusRepository(context),
            NullLogger<OrderService>.Instance);
    }
}

public sealed class AddCartItemBeforeClearRepository : IProductBucketRepository
{
    private readonly IProductBucketRepository _inner;
    private readonly Func<Task> _mutation;
    private bool _mutationApplied;

    public AddCartItemBeforeClearRepository(
        IProductBucketRepository inner,
        Func<Task> mutation)
    {
        _inner = inner;
        _mutation = mutation;
    }

    public Task<ProductBucket?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => _inner.GetByUserIdAsync(userId, cancellationToken);

    public Task<ProductBucket?> GetByIdAsync(Guid bucketId)
        => _inner.GetByIdAsync(bucketId);

    public Task<ProductBucketItem?> GetItemAsync(Guid bucketId, Guid productId)
        => _inner.GetItemAsync(bucketId, productId);

    public Task AddAsync(ProductBucket bucket)
        => _inner.AddAsync(bucket);

    public Task UpdateAsync(ProductBucket bucket)
        => _inner.UpdateAsync(bucket);

    public Task AddItemAsync(ProductBucketItem item)
        => _inner.AddItemAsync(item);

    public Task RemoveItemAsync(ProductBucketItem item)
        => _inner.RemoveItemAsync(item);

    public Task ClearAsync(Guid bucketId)
        => _inner.ClearAsync(bucketId);

    public async Task<bool> TryClearAsync(
        Guid bucketId,
        IReadOnlyCollection<ProductBucketItemCheckoutSnapshot> expectedItems,
        CancellationToken cancellationToken = default)
    {
        if (!_mutationApplied)
        {
            _mutationApplied = true;
            await _mutation();
        }

        return await _inner.TryClearAsync(
            bucketId,
            expectedItems,
            cancellationToken);
    }
}

public static class AsyncTestCapture
{
    public static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
