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

    public async Task<OrderStatus?> GetByIdAsync(Guid id)
    {
        return await _dbContext.OrderStatuses .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<OrderStatus?> GetByNameAsync(string name)
    {
        return await _dbContext.OrderStatuses .FirstOrDefaultAsync(x => x.Name == name);
    }

    public async Task<List<OrderStatus>> GetAllAsync()
    {
        return await _dbContext.OrderStatuses .ToListAsync();
    }
}
