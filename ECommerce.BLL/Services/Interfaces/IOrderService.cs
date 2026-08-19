using ECommerce.DAL.DTOs.Order;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IOrderService
    {
        Task<List<GetOrderDto>> GetAllOrdersAsync();
        Task<GetOrderDto?> GetOrderByIdAsync(Guid orderId);
        Task<List<GetOrderDto>> GetUserOrdersAsync(Guid userId);
        Task<GetOrderDto> CheckoutAsync(Guid userId, CheckoutDto checkoutDto);
        Task CancelOrderAsync(Guid userId, Guid orderId);
    }
}