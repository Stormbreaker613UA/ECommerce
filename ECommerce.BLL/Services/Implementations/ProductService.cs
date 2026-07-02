using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Product;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<List<GetProductDto>> GetAllProductsAsync()
    {
        var products = await _productRepository.GetAllAsync();

        return products.Select(MapToDto).ToList();
    }

    public async Task<GetProductDetailsDto?> GetProductByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            return null;

        return MapToDetailsDto(product);
    }

    public async Task<List<GetProductDto>> GetProductsByCategoryAsync(Guid categoryId)
    {
        var products = await _productRepository.GetByCategoryAsync(categoryId);

        return products.Select(MapToDto).ToList();
    }

    public async Task<List<GetProductDto>> SearchProductsAsync(string keyword)
    {
        var products = await _productRepository.SearchAsync(keyword);

        return products.Select(MapToDto).ToList();
    }

    public async Task<List<GetProductDto>> GetAvailableProductsAsync()
    {
        var products = await _productRepository.GetAvailableAsync();

        return products.Select(MapToDto).ToList();
    }

    public async Task<GetProductDetailsDto> AddProductAsync(CreateProductDto dto)
    {
        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);

        if (category == null)
            throw new KeyNotFoundException("Category not found.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId
        };

        foreach (var imageUrl in dto.ImageUrls)
        {
            product.ProductImages.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ImageUrl = imageUrl
            });
        }

        await _productRepository.AddAsync(product);

        return MapToDetailsDto(product);
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
            throw new KeyNotFoundException("Product not found.");

        if (dto.Name != null)
            product.Name = dto.Name;

        if (dto.Description != null)
            product.Description = dto.Description;

        if (dto.Price.HasValue)
            product.Price = dto.Price.Value;

        if (dto.StockQuantity.HasValue)
            product.StockQuantity = dto.StockQuantity.Value;

        if (dto.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value);

            if (category == null)
                throw new KeyNotFoundException("Category not found.");

            product.CategoryId = dto.CategoryId.Value;
        }

        if (dto.ImageUrls != null)
        {
            product.ProductImages.Clear();

            foreach (var imageUrl in dto.ImageUrls)
            {
                product.ProductImages.Add(new ProductImage
                {
                    Id = Guid.NewGuid(),
                    ImageUrl = imageUrl
                });
            }
        }

        await _productRepository.UpdateAsync(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        await _productRepository.DeleteAsync(id);
    }

    private static GetProductDto MapToDto(Product product)
    {
        return new GetProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            ImageUrls = product.ProductImages
                .Select(x => x.ImageUrl)
                .ToList()
        };
    }

    private static GetProductDetailsDto MapToDetailsDto(Product product)
    {
        return new GetProductDetailsDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            ImageUrls = product.ProductImages
                .Select(x => x.ImageUrl)
                .ToList()
        };
    }
}