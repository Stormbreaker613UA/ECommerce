using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(Guid id);
        Task<Order> AddOrderAsync(Order order);
        Task UpdateOrderAsync(Guid id, Order order);
        Task DeleteOrderAsync(Guid id);
    }
}