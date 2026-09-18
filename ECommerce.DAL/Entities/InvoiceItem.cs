namespace ECommerce.DAL.Entities;

// Financial line snapshot. It has no Product navigation by design: current
// product data must never alter an issued invoice.
public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public virtual Invoice Invoice { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
