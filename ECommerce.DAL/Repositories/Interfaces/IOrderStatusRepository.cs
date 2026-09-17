using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IOrderStatusRepository
{
    Task<OrderStatus?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OrderStatus?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<List<OrderStatus>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
