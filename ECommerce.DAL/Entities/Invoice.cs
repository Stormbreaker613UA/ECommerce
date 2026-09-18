namespace ECommerce.DAL.Entities;

public class Invoice : AuditableEntity
{
    public Guid OrderId { get; set; }
    // Nullable only for pre-migration historical rows. New invoices always
    // receive the completed payment id from InvoiceService.
    public Guid? PaymentId { get; set; }
    public string PaymentTransactionId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;

    // Billing snapshot
    public string BillingEmail { get; set; } = string.Empty;
    public string BillingFirstName { get; set; } = string.Empty;
    public string BillingLastName { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;

    // Amounts
    public decimal SubtotalAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; } // 0.20 = 20% VAT

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation
    public virtual Order Order { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
