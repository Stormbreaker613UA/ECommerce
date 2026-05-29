namespace ECommerce.DAL.Entities;

public class PaymentMethod : BaseEntity
{
    public string Method { get; set; } = string.Empty;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
