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
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderStatus)
            .ToListAsync();
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _dbContext.Orders
            .Include(o => o.OrderStatus)
            .ToListAsync();
    }

    public async Task<List<Order>> GetByStatusAsync(Guid statusId)
    {
        return await _dbContext.Orders
            .Where(o => o.OrderStatusId == statusId)
            .ToListAsync();
    }

    public async Task AddAsync(Order order)
    {
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Order order)
    {
        _dbContext.Orders.Update(order);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _dbContext.Orders.FindAsync(id);

        if (order == null)
            throw new KeyNotFoundException("Order not found.");

        _dbContext.Orders.Remove(order);

        await _dbContext.SaveChangesAsync();
    }
}