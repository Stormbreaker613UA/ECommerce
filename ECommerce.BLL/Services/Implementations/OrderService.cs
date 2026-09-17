using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.Order;
using ECommerce.DAL.DTOs.ProductBucket;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ECommerce.BLL.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductBucketRepository _productBucketRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ECommerceDbContext dbContext,
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IAddressRepository addressRepository,
        IProductRepository productRepository,
        IProductBucketRepository productBucketRepository,
        IOrderStatusRepository orderStatusRepository,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _productRepository = productRepository;
        _productBucketRepository = productBucketRepository;
        _orderStatusRepository = orderStatusRepository;
        _logger = logger;
    }

    public async Task<GetOrderDto> CheckoutAsync(
        Guid userId,
        CheckoutDto checkoutDto,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            ArgumentNullException.ThrowIfNull(checkoutDto);

            transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            var user = await _userRepository.GetByIdAsync(
                userId,
                cancellationToken);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var address = await _addressRepository.GetByIdAsync(
                checkoutDto.AddressId,
                cancellationToken);

            if (address == null)
                throw new KeyNotFoundException("Address not found.");

            if (address.UserId != userId)
                throw new KeyNotFoundException("Address not found.");

            var bucket = await _productBucketRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

            if (bucket == null)
                throw new InvalidOperationException("Cart not found.");

            var bucketItems = bucket.ProductBucketItems.ToList();

            if (bucketItems.Count == 0)
                throw new InvalidOperationException("Cart is empty.");

            var checkoutSnapshot = bucketItems
                .Select(item => new ProductBucketItemCheckoutSnapshot(
                    item.Id,
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.UpdatedAt))
                .ToArray();

            decimal totalAmount = 0;

            foreach (var item in bucketItems)
            {
                if (item.Quantity <= 0)
                {
                    throw new ArgumentException(
                        $"Cart quantity for product '{item.ProductId}' must be greater than zero.");
                }

                if (item.Product == null)
                {
                    throw new KeyNotFoundException(
                        $"Product '{item.ProductId}' was not found.");
                }

                if (item.Quantity > item.Product.StockQuantity)
                {
                    throw new InvalidOperationException(
                        $"Not enough stock for {item.Product.Name}.");
                }

                totalAmount += item.Quantity * item.UnitPrice;
            }

            var pendingStatus = await _orderStatusRepository.GetByNameAsync(
                "Pending",
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "Pending status not found.");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                OrderStatusId = pendingStatus.Id,
                OrderStatus = pendingStatus
            };

            foreach (var item in bucketItems)
            {
                var stockReduced = await _productRepository.TryReduceStockAsync(
                    item.ProductId,
                    item.Quantity,
                    cancellationToken);

                if (!stockReduced)
                {
                    throw new InvalidOperationException(
                        $"Stock changed for {item.Product.Name}; please retry checkout.");
                }

                order.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    Product = item.Product,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            await _orderRepository.AddAsync(order, cancellationToken);

            var cartCleared = await _productBucketRepository.TryClearAsync(
                bucket.Id,
                checkoutSnapshot,
                cancellationToken);

            if (!cartCleared)
            {
                throw new InvalidOperationException(
                    "The cart changed during checkout; please retry.");
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Order checkout succeeded for user {UserId}. Order {OrderId} created with {ItemCount} items.",
                userId,
                order.Id,
                order.OrderItems.Count);

            return MapToDto(order);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollbackAsync(transaction, userId, null);

            _logger.LogDebug(
                "Order checkout was cancelled because the request was aborted for user {UserId}.",
                userId);

            throw;
        }
        catch (OperationCanceledException ex)
        {
            await RollbackAsync(transaction, userId, null);

            _logger.LogWarning(
                ex,
                "Order checkout was cancelled before completion for user {UserId}.",
                userId);

            throw;
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction, userId, null);

            _logger.LogWarning(
                ex,
                "Order checkout failed for user {UserId}.",
                userId);

            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<List<GetOrderDto>> GetAllOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    public async Task<GetOrderDto?> GetOrderByIdAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdForUserAsync(
            orderId,
            userId,
            cancellationToken);

        return order == null
            ? null
            : MapToDto(order);
    }

    public async Task<GetOrderDto?> GetOrderByIdForAdminAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            orderId,
            cancellationToken);

        return order == null
            ? null
            : MapToDto(order);
    }

    public async Task<List<GetOrderDto>> GetUserOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    public async Task CancelOrderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            var order = await _orderRepository.GetByIdForCancellationAsync(
                orderId,
                cancellationToken);

            if (order == null)
                throw new KeyNotFoundException("Order not found.");

            if (order.UserId != userId)
                throw new KeyNotFoundException("Order not found.");

            if (order.OrderStatus == null ||
                !order.OrderStatus.CanCancel ||
                order.OrderStatus.Name.Equals(
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Order cannot be cancelled from its current status.");
            }

            var cancelledStatus = await _orderStatusRepository.GetByNameAsync(
                "Cancelled",
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "Cancelled status not found.");

            foreach (var item in order.OrderItems)
            {
                if (item.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        $"Order item '{item.Id}' has an invalid quantity.");
                }

                if (item.Product == null)
                {
                    throw new KeyNotFoundException(
                        $"Product '{item.ProductId}' was not found while cancelling the order.");
                }
            }

            var statusChanged = await _dbContext.Orders
                .Where(candidate =>
                    candidate.Id == orderId &&
                    candidate.UserId == userId &&
                    candidate.OrderStatusId == order.OrderStatusId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            candidate => candidate.OrderStatusId,
                            cancelledStatus.Id)
                        .SetProperty(
                            candidate => candidate.UpdatedAt,
                            DateTime.UtcNow),
                    cancellationToken);

            if (statusChanged != 1)
            {
                throw new InvalidOperationException(
                    "Order status changed before cancellation completed.");
            }

            foreach (var item in order.OrderItems)
            {
                await _productRepository.RestoreStockAsync(
                    item.ProductId,
                    item.Quantity,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Order cancellation succeeded for user {UserId}. Order {OrderId} changed to Cancelled and stock was restored.",
                userId,
                orderId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollbackAsync(transaction, userId, orderId);

            _logger.LogDebug(
                "Order cancellation was cancelled because the request was aborted for user {UserId} and order {OrderId}.",
                userId,
                orderId);

            throw;
        }
        catch (OperationCanceledException ex)
        {
            await RollbackAsync(transaction, userId, orderId);

            _logger.LogWarning(
                ex,
                "Order cancellation was cancelled before completion for user {UserId} and order {OrderId}.",
                userId,
                orderId);

            throw;
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction, userId, orderId);

            _logger.LogWarning(
                ex,
                "Order cancellation failed for user {UserId} and order {OrderId}.",
                userId,
                orderId);

            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    private async Task RollbackAsync(
        IDbContextTransaction? transaction,
        Guid userId,
        Guid? orderId)
    {
        if (transaction == null)
            return;

        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(
                rollbackException,
                "Order transaction rollback failed for user {UserId} and order {OrderId}.",
                userId,
                orderId);
        }
    }

    private static GetOrderDto MapToDto(Order order)
    {
        return new GetOrderDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Status = order.OrderStatus?.Name ?? string.Empty,
            Items = order.OrderItems
                .Select(x => new GetOrderItemDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.Product?.Name ?? string.Empty,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.UnitPrice * x.Quantity
                })
                .ToList()
        };
    }
}
