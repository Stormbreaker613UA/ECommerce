namespace ECommerce.DAL.Entities;

public class Invoice : AuditableEntity
{
    public Guid OrderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty; // INV-2025-000001

    // Billing snapshot
    public string BillingEmail { get; set; } = string.Empty;
    public string BillingFirstName { get; set; } = string.Empty;
    public string BillingLastName { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;

    // Amounts
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxRate { get; set; } // 0.20 = 20% VAT

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation
    public virtual Order Order { get; set; } = null!;
}
