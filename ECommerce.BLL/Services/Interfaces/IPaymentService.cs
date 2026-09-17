using ECommerce.DAL.DTOs.Payment;

namespace ECommerce.BLL.Services.Interfaces;

public interface IPaymentService
{
    Task<List<PaymentResponseDto>> GetAllPaymentsAsync(
        CancellationToken cancellationToken = default);

    Task<PaymentResponseDto?> GetPaymentByIdAsync(
        Guid paymentId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PaymentResponseDto> AddPaymentAsync(
        Guid userId,
        CreatePaymentRequestDto request,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentAsync(
        Guid paymentId,
        Guid userId,
        bool isAdministrator,
        UpdatePaymentRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PaymentResponseDto> CompletePaymentAsync(
        Guid paymentId,
        Guid actorId,
        CompletePaymentRequestDto request,
        CancellationToken cancellationToken = default);

    Task DeletePaymentAsync(
        Guid paymentId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}
