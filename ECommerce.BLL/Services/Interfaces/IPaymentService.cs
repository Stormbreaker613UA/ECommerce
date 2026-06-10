using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<List<Payment>> GetAllPaymentsAsync();
        Task<Payment?> GetPaymentByIdAsync(Guid id);
        Task<Payment> AddPaymentAsync(Payment payment);
        Task UpdatePaymentAsync(Guid id, Payment payment);
        Task DeletePaymentAsync(Guid id);
    }
}