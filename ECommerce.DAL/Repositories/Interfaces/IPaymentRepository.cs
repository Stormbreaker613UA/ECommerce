using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(Guid id);

        Task<Payment?> GetByOrderIdAsync(Guid orderId);

        Task<List<Payment>> GetAllAsync();

        Task<List<Payment>> GetByStatusAsync(Guid paymentStatusId);

        Task AddAsync(Payment payment);

        Task UpdateAsync(Payment payment);
    }
}
