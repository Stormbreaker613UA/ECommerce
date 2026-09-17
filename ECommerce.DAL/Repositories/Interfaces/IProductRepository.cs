using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetAllAsync();

    Task<List<Product>> GetByCategoryAsync(Guid categoryId);

    Task<List<Product>> SearchAsync(string keyword);

    Task<List<Product>> GetAvailableAsync();

    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);

    Task<bool> TryReduceStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task RestoreStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default);
}
