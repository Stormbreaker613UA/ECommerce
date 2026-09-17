using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Payment?> GetByIdForUserAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<Payment?> GetByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<Payment?> GetByCompletionIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<Payment?> GetByOrderIdAsync(
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<List<Payment>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<List<Payment>> GetByStatusAsync(
            Guid paymentStatusId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Payment payment,
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Payment payment,
            CancellationToken cancellationToken = default);

        Task<int> TryCompleteAsync(
            Guid paymentId,
            Guid expectedStatusId,
            Guid completedStatusId,
            string completionIdempotencyKey,
            string transactionId,
            DateTime paidAt,
            Guid updatedBy,
            CancellationToken cancellationToken = default);

        Task<int> TryMarkFailedAsync(
            Guid paymentId,
            Guid expectedStatusId,
            Guid failedStatusId,
            DateTime updatedAt,
            Guid updatedBy,
            CancellationToken cancellationToken = default);

        Task<int> TryUpdatePaymentMethodAsync(
            Guid paymentId,
            Guid expectedStatusId,
            Guid paymentMethodId,
            DateTime updatedAt,
            Guid updatedBy,
            CancellationToken cancellationToken = default);
    }
}
