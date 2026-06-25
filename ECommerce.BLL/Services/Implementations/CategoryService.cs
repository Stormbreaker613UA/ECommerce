using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Category;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<GetCategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        return categories
            .Select(MapToDto)
            .ToList();
    }

    public async Task<GetCategoryDto?> GetByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
        {
            return null;
        }

        return MapToDto(category);
    }

    public async Task<List<GetCategoryDto>> GetRootCategoriesAsync()
    {
        var categories = await _categoryRepository.GetRootCategoriesAsync();

        return categories
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<GetCategoryDto>> GetSubCategoriesAsync(Guid parentId)
    {
        var parentCategory = await _categoryRepository.GetByIdAsync(parentId);

        if (parentCategory == null)
        {
            throw new KeyNotFoundException("Parent category not found.");
        }

        var categories = await _categoryRepository.GetSubCategoriesAsync(parentId);

        return categories
            .Select(MapToDto)
            .ToList();
    }

    public async Task<GetCategoryTreeDto?> GetWithChildrenAsync(Guid id)
    {
        var category = await _categoryRepository.GetWithChildrenAsync(id);

        if (category == null)
        {
            return null;
        }

        return MapToTreeDto(category);
    }

    public async Task<GetCategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        ValidateCategoryData(dto.Name, dto.Description);

        if (dto.ParentCategoryId.HasValue)
        {
            var parentCategory = await _categoryRepository.GetByIdAsync(dto.ParentCategoryId.Value);

            if (parentCategory == null)
            {
                throw new KeyNotFoundException("Parent category not found.");
            }
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            ParentCategoryId = dto.ParentCategoryId
        };

        await _categoryRepository.AddAsync(category);

        return MapToDto(category);
    }

    public async Task UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        ValidateCategoryData(dto.Name, dto.Description);

        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category not found.");

        await ValidateParentAsync(id, dto.ParentCategoryId);

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;

        await _categoryRepository.UpdateAsync(category);
    }

    public async Task UpdateParentAsync(Guid id, UpdateCategoryParentDto dto)
    {
        var category = await _categoryRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category not found.");

        await ValidateParentAsync(id, dto.ParentCategoryId);

        category.ParentCategoryId = dto.ParentCategoryId;

        await _categoryRepository.UpdateAsync(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
        {
            throw new KeyNotFoundException("Category not found.");
        }

        await _categoryRepository.DeleteAsync(id);
    }

    private async Task ValidateParentAsync(Guid categoryId, Guid? parentCategoryId)
    {
        if (parentCategoryId == null)
        {
            return;
        }

        if (parentCategoryId.Value == categoryId)
        {
            throw new InvalidOperationException("Category cannot be parent of itself.");
        }

        var parentCategory = await _categoryRepository.GetByIdAsync(parentCategoryId.Value);

        if (parentCategory == null)
        {
            throw new KeyNotFoundException("Parent category not found.");
        }

        var currentParent = parentCategory;

        while (currentParent.ParentCategoryId.HasValue)
        {
            if (currentParent.ParentCategoryId.Value == categoryId)
            {
                throw new InvalidOperationException("Cannot move category inside its own child category.");
            }

            currentParent = await _categoryRepository.GetByIdAsync(currentParent.ParentCategoryId.Value);

            if (currentParent == null)
            {
                break;
            }
        }
    }

    private static void ValidateCategoryData(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.");
        }

        if (name.Length > 100)
        {
            throw new ArgumentException("Category name cannot be longer than 100 characters.");
        }

        if (description?.Length > 500)
        {
            throw new ArgumentException("Category description cannot be longer than 500 characters.");
        }
    }

    private static GetCategoryDto MapToDto(Category category)
    {
        return new GetCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name
        };
    }

    private static GetCategoryTreeDto MapToTreeDto(Category category)
    {
        return new GetCategoryTreeDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            SubCategories = category.SubCategories
                .Select(MapToTreeDto)
                .ToList()
        };
    }
}