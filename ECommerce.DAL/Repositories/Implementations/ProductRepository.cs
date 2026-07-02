using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.DAL.Repositories.Implementations;

public class ProductRepository : IProductRepository
{
    private readonly ECommerceDbContext _dbContext;

    public ProductRepository(ECommerceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .ToListAsync();
    }

    public async Task<List<Product>> GetByCategoryAsync(Guid categoryId)
    {
        return await _dbContext.Products
            .Where(p => p.CategoryId == categoryId)
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .ToListAsync();
    }

    public async Task<List<Product>> SearchAsync(string keyword)
    {
        return await _dbContext.Products
       .Include(p => p.Category)
       .Include(p => p.ProductImages)
       .Where(p => p.Name.Contains(keyword) ||
                   p.Description.Contains(keyword))
       .ToListAsync();
    }

    public async Task<List<Product>> GetAvailableAsync()
    {
        return await _dbContext.Products
        .Include(p => p.Category)
        .Include(p => p.ProductImages)
        .Where(p => p.StockQuantity > 0)
        .ToListAsync();
    }

    public async Task AddAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _dbContext.Products.FindAsync(id);

        if (product == null)
            throw new KeyNotFoundException("Product not found");

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();
    }
}