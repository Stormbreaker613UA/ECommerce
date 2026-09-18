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

    public async Task<Payment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(
                p => p.Id == id && p.Order.UserId == userId,
                cancellationToken);
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(
                p => p.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<Payment?> GetByCompletionIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .FirstOrDefaultAsync(
                p => p.CompletionIdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<Payment?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(p =>
                p.OrderId == orderId &&
                p.PaymentStatusId != PaymentStatusCatalog.FailedId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Payment>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Payment>> GetByStatusAsync(
        Guid paymentStatusId,
        CancellationToken cancellationToken = default)
    {
        return await QueryWithDetails()
            .Where(p => p.PaymentStatusId == paymentStatusId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Payments.AddAsync(payment, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> TryCompleteAsync(
        Guid paymentId,
        Guid expectedStatusId,
        Guid completedStatusId,
        string completionIdempotencyKey,
        string transactionId,
        DateTime paidAt,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Where(p =>
                p.Id == paymentId &&
                p.PaymentStatusId == expectedStatusId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.PaymentStatusId, completedStatusId)
                    .SetProperty(p => p.CompletionIdempotencyKey, completionIdempotencyKey)
                    .SetProperty(p => p.TransactionId, transactionId)
                    .SetProperty(p => p.PaidAt, (DateTime?)paidAt)
                    .SetProperty(p => p.UpdatedAt, (DateTime?)paidAt)
                    .SetProperty(p => p.UpdatedBy, (Guid?)updatedBy),
                cancellationToken);
    }

    public async Task<int> TryMarkFailedAsync(
        Guid paymentId,
        Guid expectedStatusId,
        Guid failedStatusId,
        DateTime updatedAt,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Where(p =>
                p.Id == paymentId &&
                p.PaymentStatusId == expectedStatusId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.PaymentStatusId, failedStatusId)
                    .SetProperty(p => p.UpdatedAt, (DateTime?)updatedAt)
                    .SetProperty(p => p.UpdatedBy, (Guid?)updatedBy),
                cancellationToken);
    }

    public async Task<int> TryUpdatePaymentMethodAsync(
        Guid paymentId,
        Guid expectedStatusId,
        Guid paymentMethodId,
        DateTime updatedAt,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Payments
            .Where(p =>
                p.Id == paymentId &&
                p.PaymentStatusId == expectedStatusId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.PaymentMethodId, paymentMethodId)
                    .SetProperty(p => p.UpdatedAt, (DateTime?)updatedAt)
                    .SetProperty(p => p.UpdatedBy, (Guid?)updatedBy),
                cancellationToken);
    }

    private IQueryable<Payment> QueryWithDetails()
    {
        return _dbContext.Payments
            .AsNoTracking()
            .Include(p => p.Order)
                .ThenInclude(o => o.OrderStatus)
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod);
    }
}
