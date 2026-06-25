namespace ECommerce.DAL.DTOs.Category;

public class GetCategoryTreeDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? ParentCategoryId { get; set; }

    public List<GetCategoryTreeDto> SubCategories { get; set; } = new();
}
