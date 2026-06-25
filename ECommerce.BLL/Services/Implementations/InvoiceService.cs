using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _invoiceRepository.GetAllAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(Guid id)
        {
            return await _invoiceRepository.GetByIdAsync(id);
        }

        public async Task<Invoice> AddInvoiceAsync(Invoice invoice)
        {
            await _invoiceRepository.AddAsync(invoice);
            return invoice;
        }

        public async Task UpdateInvoiceAsync(Guid id, Invoice invoice)
        {
            var existing = await _invoiceRepository.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("Invoice not found");

            existing.OrderId = invoice.OrderId;
            existing.TotalAmount = invoice.TotalAmount;

            await _invoiceRepository.UpdateAsync(existing);
        }

        public async Task DeleteInvoiceAsync(Guid id)
        {
            await _invoiceRepository.DeleteAsync(id);
        }
    }
}