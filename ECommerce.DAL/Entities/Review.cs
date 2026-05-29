namespace ECommerce.DAL.Entities;

public class Review : AuditableEntity
{
    public Guid ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public int Rating { get; set; }
    public string CommentText { get; set; } = string.Empty;
}
