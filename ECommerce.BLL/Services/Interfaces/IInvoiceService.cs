using ECommerce.DAL.DTOs.Invoice;

namespace ECommerce.BLL.Services.Interfaces;

public interface IInvoiceService
{
    // Domain operation used by PaymentService inside its transaction; there is
    // deliberately no public invoice creation endpoint.
    Task<InvoiceResponseDto> EnsureInvoiceForCompletedPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponseDto?> GetInvoiceByIdAsync(
        Guid invoiceId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponseDto?> GetInvoiceByOrderIdAsync(
        Guid orderId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<InvoicePageResponseDto> GetInvoicesAsync(
        Guid? userId,
        bool isAdministrator,
        InvoicePageRequestDto request,
        CancellationToken cancellationToken = default);
}
