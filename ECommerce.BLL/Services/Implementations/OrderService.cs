using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Order;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductBucketRepository _productBucketRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IAddressRepository addressRepository,
        IProductRepository productRepository,
        IProductBucketRepository productBucketRepository,
        IOrderStatusRepository orderStatusRepository)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _productRepository = productRepository;
        _productBucketRepository = productBucketRepository;
        _orderStatusRepository = orderStatusRepository;
    }

    public async Task<GetOrderDto> CheckoutAsync(
        Guid userId,
        CheckoutDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var address = await _addressRepository.GetByIdAsync(dto.AddressId);

        if (address == null)
            throw new KeyNotFoundException("Address not found.");

        if (address.UserId != userId)
            throw new UnauthorizedAccessException();

        var bucket =
            await _productBucketRepository.GetByUserIdAsync(userId);

        if (bucket == null)
            throw new InvalidOperationException("Cart not found.");

        if (!bucket.ProductBucketItems.Any())
            throw new InvalidOperationException("Cart is empty.");

        decimal totalAmount = 0;

        foreach (var item in bucket.ProductBucketItems)
        {
            if (item.Quantity > item.Product.StockQuantity)
            {
                throw new InvalidOperationException(
                    $"Not enough stock for {item.Product.Name}");
            }

            totalAmount += item.Quantity * item.UnitPrice;
        }

        var pendingStatus =
            await _orderStatusRepository.GetByNameAsync("Pending")
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

        foreach (var item in bucket.ProductBucketItems)
        {
            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });

            item.Product.StockQuantity -= item.Quantity;

            await _productRepository.UpdateAsync(item.Product);
        }

        await _orderRepository.AddAsync(order);

        await _productBucketRepository.ClearAsync(bucket.Id);

        return MapToDto(order);
    }

    public async Task<List<GetOrderDto>> GetAllOrdersAsync()
    {
        var orders = await _orderRepository.GetAllAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<GetOrderDto?> GetOrderByIdAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);

        return order == null
            ? null
            : MapToDto(order);
    }

    public async Task<List<GetOrderDto>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId);

        return orders.Select(MapToDto).ToList();
    }

    public async Task CancelOrderAsync(
        Guid userId,
        Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null)
            throw new KeyNotFoundException("Order not found.");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException();

        if (!order.OrderStatus.CanCancel)
        {
            throw new InvalidOperationException(
                "Order cannot be cancelled.");
        }

        foreach (var item in order.OrderItems)
        {
            var product =
                await _productRepository.GetByIdAsync(item.ProductId);

            if (product == null)
                continue;

            product.StockQuantity += item.Quantity;

            await _productRepository.UpdateAsync(product);
        }

        var cancelledStatus =
            await _orderStatusRepository.GetByNameAsync("Cancelled")
            ?? throw new InvalidOperationException(
                "Cancelled status not found.");

        order.OrderStatusId = cancelledStatus.Id;
        order.OrderStatus = cancelledStatus;

        await _orderRepository.UpdateAsync(order);
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