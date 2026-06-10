using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(Guid id);
        Task<Invoice> AddInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Guid id, Invoice invoice);
        Task DeleteInvoiceAsync(Guid id);
    }
}