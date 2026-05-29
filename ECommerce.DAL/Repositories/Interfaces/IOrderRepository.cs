using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetByUserIdAsync(Guid userId);
    Task<List<Order>> GetAllAsync();

    Task AddAsync(Order order);

    Task<List<Order>> GetByStatusAsync(Guid statusId);
}
