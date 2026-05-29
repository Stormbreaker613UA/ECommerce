using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(Guid id);

        Task<List<Category>> GetRootCategoriesAsync();
        Task<List<Category>> GetSubCategoriesAsync(Guid parentId);

        Task<Category?> GetWithChildrenAsync(Guid id);

        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(Guid id);
    }
}
