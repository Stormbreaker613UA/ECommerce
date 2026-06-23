using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _orderRepository.GetAllAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(Guid id)
        {
            return await _orderRepository.GetByIdAsync(id);
        }

        public async Task<Order> AddOrderAsync(Order order)
        {
            await _orderRepository.AddAsync(order);
            return order;
        }

        public async Task UpdateOrderAsync(Guid id, Order order)
        {
            var existing = await _orderRepository.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("Order not found");

            existing.UserId = order.UserId;
            existing.Total = order.Total;
            existing.OrderStatus = order.OrderStatus;

            await _orderRepository.UpdateAsync(existing);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            await _orderRepository.DeleteAsync(id);
        }
    }
}