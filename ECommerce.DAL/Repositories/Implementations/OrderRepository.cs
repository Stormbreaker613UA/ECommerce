using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class OrderRepository : IOrderRepository
{
    private readonly ECommerceDbContext _dbContext;

    public OrderRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Orders.FindAsync(id);
    }

    public async Task<List<Order>> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.Orders
            .Where(o => o.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _dbContext.Orders.ToListAsync();
    }

    public async Task AddAsync(Order order)
    {
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<Order>> GetByStatusAsync(Guid orderStatusId)
    {
        return await _dbContext.Orders
            .Where(o => o.OrderStatusId == orderStatusId)
            .ToListAsync();
    }
}