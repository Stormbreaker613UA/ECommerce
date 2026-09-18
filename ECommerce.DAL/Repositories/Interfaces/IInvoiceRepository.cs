using ECommerce.DAL.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetByOrderIdForUserAsync(
        Guid orderId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<(List<Invoice> Items, int TotalCount)> GetPageAsync(
        Guid? userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
