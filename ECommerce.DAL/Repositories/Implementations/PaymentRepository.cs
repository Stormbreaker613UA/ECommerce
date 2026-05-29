using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class PaymentRepository : IPaymentRepository
{
    private readonly ECommerceDbContext _dbContext;

    public PaymentRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Payments
            .Include(p => p.Order)
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId)
    {
        return await _dbContext.Payments
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<List<Payment>> GetAllAsync()
    {
        return await _dbContext.Payments
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetByStatusAsync(Guid paymentStatusId)
    {
        return await _dbContext.Payments
            .Where(p => p.PaymentStatusId == paymentStatusId)
            .ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        await _dbContext.Payments.AddAsync(payment);
    }

    public Task UpdateAsync(Payment payment)
    {
        _dbContext.Payments.Update(payment);
        return Task.CompletedTask;
    }
}
