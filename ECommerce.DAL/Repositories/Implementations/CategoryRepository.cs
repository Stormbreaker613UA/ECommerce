using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class CategoryRepository : ICategoryRepository
{
    private readonly ECommerceDbContext _dbContext;

    public CategoryRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Category>> GetRootCategoriesAsync()
    {
        return await _dbContext.Categories
            .Where(c => c.ParentCategoryId == null)
            .ToListAsync();
    }

    public async Task<List<Category>> GetSubCategoriesAsync(Guid parentId)
    {
        return await _dbContext.Categories
            .Where(c => c.ParentCategoryId == parentId)
            .ToListAsync();
    }

    public async Task<Category?> GetWithChildrenAsync(Guid id)
    {
        return await _dbContext.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Category category)
    {
        await _dbContext.Categories.AddAsync(category);
    }

    public Task UpdateAsync(Category category)
    {
        _dbContext.Categories.Update(category);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category != null)
            _dbContext.Categories.Remove(category);
    }
}