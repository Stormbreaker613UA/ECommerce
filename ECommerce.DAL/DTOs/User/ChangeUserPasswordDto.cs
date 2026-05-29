namespace ECommerce.DAL.DTOs.User;

public class ChangeUserPasswordDto
{
    public Guid UserId { get; set; }
    public string CurrentPasswordHash { get; set; } = string.Empty;
    public string NewPasswordHash { get; set; } = string.Empty;
}
