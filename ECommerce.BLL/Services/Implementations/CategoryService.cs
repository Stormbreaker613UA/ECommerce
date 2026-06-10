using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(Guid id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            await _categoryRepository.AddAsync(category);
            return category;
        }

        public async Task UpdateCategoryAsync(Guid id, Category category)
        {
            var existing = await _categoryRepository.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("Category not found");

            existing.Name = category.Name;
            existing.Description = category.Description;

            await _categoryRepository.UpdateAsync(existing);
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            await _categoryRepository.DeleteAsync(id);
        }
    }
}