using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IOrderStatusRepository
{
    Task<OrderStatus?> GetByIdAsync(Guid id);

    Task<OrderStatus?> GetByNameAsync(string name);

    Task<List<OrderStatus>> GetAllAsync();
}
