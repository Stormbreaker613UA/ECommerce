using ECommerce.DAL.DTOs.Order;

namespace ECommerce.BLL.Services.Interfaces
{
public interface IOrderService
{
    Task<List<GetOrderDto>> GetAllOrdersAsync(
        CancellationToken cancellationToken = default);

    Task<GetOrderDto?> GetOrderByIdAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<GetOrderDto?> GetOrderByIdForAdminAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<GetOrderDto>> GetUserOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<GetOrderDto> CheckoutAsync(
        Guid userId,
        CheckoutDto checkoutDto,
        CancellationToken cancellationToken = default);

    Task CancelOrderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
}
