namespace ECommerce.DAL.Entities;

public class PaymentStatus : BaseEntity
{
    public string Status { get; set; } = string.Empty;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

}
