using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;

        public PaymentService(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<List<Payment>> GetAllPaymentsAsync()
        {
            return await _paymentRepository.GetAllAsync();
        }

        public async Task<Payment?> GetPaymentByIdAsync(Guid id)
        {
            return await _paymentRepository.GetByIdAsync(id);
        }

        public async Task<Payment> AddPaymentAsync(Payment payment)
        {
            await _paymentRepository.AddAsync(payment);
            return payment;
        }

        public async Task UpdatePaymentAsync(Guid id, Payment payment)
        {
            var existing = await _paymentRepository.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("Payment not found");

            existing.Amount = payment.Amount;
            existing.PaymentMethodId = payment.PaymentMethodId;
            existing.PaymentStatusId = payment.PaymentStatusId;
            await _paymentRepository.UpdateAsync(existing);
        }

        public async Task DeletePaymentAsync(Guid id)
        {
            throw new NotImplementedException(); // todo
        }
    }
}