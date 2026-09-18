namespace ECommerce.DAL.DTOs.Payment;

public sealed class CreatePaymentRequestDto
{
    public Guid OrderId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
