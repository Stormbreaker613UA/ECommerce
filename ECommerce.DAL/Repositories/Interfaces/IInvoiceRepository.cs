using ECommerce.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<List<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId);
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task AddAsync(Invoice invoice);
}