using ECommerce.DAL.DTOs.Category;

namespace ECommerce.BLL.Services.Interfaces;

public interface ICategoryService
{
    Task<List<GetCategoryDto>> GetAllAsync();

    Task<GetCategoryDto?> GetByIdAsync(Guid id);

    Task<List<GetCategoryDto>> GetRootCategoriesAsync();

    Task<List<GetCategoryDto>> GetSubCategoriesAsync(Guid parentId);

    Task<GetCategoryTreeDto?> GetWithChildrenAsync(Guid id);

    Task<GetCategoryDto> CreateAsync(CreateCategoryDto dto);

    Task UpdateAsync(Guid id, UpdateCategoryDto dto);

    Task UpdateParentAsync(Guid id, UpdateCategoryParentDto dto);

    Task DeleteAsync(Guid id);
}