using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ECommerceDbContext _dbContext;

    public InvoiceRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Invoice?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(
                invoice => invoice.Id == id && invoice.Order.UserId == userId,
                cancellationToken);
    }

    public async Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByOrderIdForUserAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(
                invoice => invoice.OrderId == orderId && invoice.Order.UserId == userId,
                cancellationToken);
    }

    public async Task<Invoice?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(invoice => invoice.OrderId == orderId, cancellationToken);
    }

    public async Task<(List<Invoice> Items, int TotalCount)> GetPageAsync(
        Guid? userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = QueryWithDetails();
        if (userId.HasValue)
            query = query.Where(invoice => invoice.Order.UserId == userId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(invoice => invoice.IssuedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Invoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Invoice> QueryWithDetails()
    {
        return _dbContext.Invoices
            // Invoice access is a financial-history read. Ignore filters only
            // for this query so a soft-deleted source Order remains available
            // for owner scoping and Invoice navigation; no other repository
            // changes Order soft-delete behavior.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(invoice => invoice.Order)
            .Include(invoice => invoice.Items);
    }
}
