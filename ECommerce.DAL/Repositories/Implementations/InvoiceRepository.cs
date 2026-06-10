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

    public async Task<List<Invoice>> GetAllAsync()
    {
        return await _dbContext.Invoices
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Invoice?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Invoices
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Invoice?> GetByOrderIdAsync(Guid orderId)
    {
        return await _dbContext.Invoices
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.OrderId == orderId);
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        return await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.InvoiceNumber == invoiceNumber);
    }

    public async Task AddAsync(Invoice invoice)
    {
        await _dbContext.Invoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync();
    }
}
