using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(System.Guid id);
        Task<Category> AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(System.Guid id, Category category);
        Task DeleteCategoryAsync(System.Guid id);
    }
}