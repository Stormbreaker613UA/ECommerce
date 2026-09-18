namespace ECommerce.DAL.DTOs.Invoice;

public sealed class InvoiceResponseDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid? PaymentId { get; init; }
    public string PaymentTransactionId { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string BillingEmail { get; init; } = string.Empty;
    public string BillingFirstName { get; init; } = string.Empty;
    public string BillingLastName { get; init; } = string.Empty;
    public string BillingAddress { get; init; } = string.Empty;
    public decimal SubtotalAmount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<InvoiceItemResponseDto> Items { get; init; } = [];
}

public sealed class InvoiceItemResponseDto
{
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}
