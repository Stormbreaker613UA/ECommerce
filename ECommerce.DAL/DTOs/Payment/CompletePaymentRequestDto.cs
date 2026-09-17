namespace ECommerce.DAL.DTOs.Payment;

public sealed class CompletePaymentRequestDto
{
    public string IdempotencyKey { get; set; } = string.Empty;
}
