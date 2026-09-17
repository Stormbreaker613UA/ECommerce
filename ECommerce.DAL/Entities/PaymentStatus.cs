namespace ECommerce.DAL.Entities;

public class PaymentStatus : BaseEntity
{
    public string Status { get; set; } = string.Empty;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

}

public static class PaymentStatusCatalog
{
    public static readonly Guid PendingId = new("c1b2c3d4-0000-0000-0000-000000000001");
    public static readonly Guid CompletedId = new("c1b2c3d4-0000-0000-0000-000000000002");
    public static readonly Guid FailedId = new("c1b2c3d4-0000-0000-0000-000000000003");

    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
