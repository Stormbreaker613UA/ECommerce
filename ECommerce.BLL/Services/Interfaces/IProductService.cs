using ECommerce.DAL.DTOs.Product;

namespace ECommerce.BLL.Services.Interfaces;

public interface IProductService
{
    Task<List<GetProductDto>> GetAllProductsAsync();
    Task<GetProductDetailsDto?> GetProductByIdAsync(Guid id);
    Task<List<GetProductDto>> GetProductsByCategoryAsync(Guid categoryId);
    Task<List<GetProductDto>> SearchProductsAsync(string keyword);
    Task<GetProductDetailsDto> AddProductAsync(CreateProductDto dto);
    Task<List<GetProductDto>> GetAvailableProductsAsync();
    Task UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
}