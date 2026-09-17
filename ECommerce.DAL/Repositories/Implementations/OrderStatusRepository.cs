using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class OrderStatusRepository : IOrderStatusRepository
{
    private readonly ECommerceDbContext _dbContext;

    public OrderStatusRepository( ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderStatus?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderStatuses
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<OrderStatus?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderStatuses
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<List<OrderStatus>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderStatuses
            .ToListAsync(cancellationToken);
    }
}
