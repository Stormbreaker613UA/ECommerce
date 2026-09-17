using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace ECommerce.DAL.Repositories.Implementations;

public class OrderRepository : IOrderRepository
{
    private readonly ECommerceDbContext _dbContext;

    public OrderRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(
                o => o.Id == id && o.UserId == userId,
                cancellationToken);
    }

    public async Task<Order?> GetByIdForCancellationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Keep the Order and OrderItem filters active. Only the Product query
        // below ignores its own filter so an order remains cancellable after a
        // product has been soft-deleted.
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.OrderStatus)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
            return null;

        var productIds = order.OrderItems
            .Select(item => item.ProductId)
            .Distinct()
            .ToArray();

        if (productIds.Length == 0)
            return order;

        var products = await _dbContext.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        foreach (var item in order.OrderItems)
        {
            if (products.TryGetValue(item.ProductId, out var product))
                item.Product = product;
        }

        return order;
    }

    public async Task<bool> LockOrderRowAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException(
                "An active transaction is required before locking an order.");

        var connection = _dbContext.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;

        if (!wasOpen)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                SELECT 1
                FROM "Orders"
                WHERE "Id" = @order_id
                  AND "IsDeleted" = FALSE
                FOR UPDATE
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@order_id";
            parameter.DbType = DbType.Guid;
            parameter.Value = id;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && result != DBNull.Value;
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }

    public async Task<List<Order>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(o => o.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Order>> GetByStatusAsync(
        Guid statusId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .Where(o => o.OrderStatusId == statusId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Orders.Update(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order == null)
            throw new KeyNotFoundException("Order not found.");

        _dbContext.Orders.Remove(order);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Order> QueryWithDetails()
    {
        return _dbContext.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
            .Include(o => o.OrderStatus);
    }
}
