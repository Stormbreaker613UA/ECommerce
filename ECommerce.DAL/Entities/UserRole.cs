namespace ECommerce.DAL.Entities;

public class UserRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}