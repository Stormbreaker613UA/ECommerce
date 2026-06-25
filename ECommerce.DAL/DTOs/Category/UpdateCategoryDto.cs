
namespace ECommerce.DAL.DTOs.Category;

public class UpdateCategoryDto
{
   
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? ParentCategoryId { get; set; }
}
