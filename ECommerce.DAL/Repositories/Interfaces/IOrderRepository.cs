using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForCancellationAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<Order>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<Order>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<List<Order>> GetByStatusAsync(
        Guid statusId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
