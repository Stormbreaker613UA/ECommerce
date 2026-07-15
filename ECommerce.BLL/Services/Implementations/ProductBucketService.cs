using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.ProductBucket;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations;

public class ProductBucketService : IProductBucketService
{
    private readonly IProductBucketRepository _productBucketRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;

    public ProductBucketService(
        IProductBucketRepository productBucketRepository,
        IProductRepository productRepository,
        IUserRepository userRepository)
    {
        _productBucketRepository = productBucketRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
    }

    public async Task AddProductAsync(Guid userId, AddProductToBucketDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var product = await _productRepository.GetByIdAsync(dto.ProductId);

        if (product == null)
            throw new KeyNotFoundException("Product not found.");

        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var bucket = await _productBucketRepository.GetByUserIdAsync(userId);

        if (bucket == null)
            throw new InvalidOperationException("Shopping cart was not found.");

        var bucketItem = await _productBucketRepository.GetItemAsync(bucket.Id, dto.ProductId);

        if (bucketItem != null)
        {
            var newQuantity = bucketItem.Quantity + dto.Quantity;

            if (newQuantity > product.StockQuantity)
                throw new InvalidOperationException("Not enough products in stock.");

            bucketItem.Quantity = newQuantity;

            await _productBucketRepository.UpdateAsync(bucket);

            return;
        }

        if (dto.Quantity > product.StockQuantity)
            throw new InvalidOperationException("Not enough products in stock.");

        var newItem = new ProductBucketItem
        {
            ProductBucketId = bucket.Id,
            ProductId = product.Id,
            Quantity = dto.Quantity,
            UnitPrice = product.Price
        };

        await _productBucketRepository.AddItemAsync(newItem);
    }

    public async Task ClearBucketAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var bucket = await _productBucketRepository.GetByUserIdAsync(userId);

        if (bucket == null)
            throw new InvalidOperationException("Shopping cart was not found.");

        await _productBucketRepository.ClearAsync(bucket.Id);
    }

    public async Task<GetProductBucketDto> GetBucketAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }
        var bucket = await _productBucketRepository.GetByUserIdAsync(userId);
        if (bucket == null)
        {
            throw new InvalidOperationException("Product bucket not found");
        }

        var bucketDto = new GetProductBucketDto
        {
            Id = bucket.Id,
            UserId = bucket.UserId,
            Items = bucket.ProductBucketItems.Select(item => new GetProductBucketItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                TotalPrice = item.UnitPrice * item.Quantity,
                ImageUrls = item.Product.ProductImages
                 .Select(image => image.ImageUrl)
                 .ToList()
            }).ToList()
        };
        return await Task.FromResult(bucketDto);
    }

    public async Task RemoveProductAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var bucket = await _productBucketRepository.GetByUserIdAsync(userId);

        if (bucket == null)
            throw new InvalidOperationException("Shopping cart was not found.");

        var bucketItem = await _productBucketRepository.GetItemAsync(bucket.Id, productId);

        if (bucketItem == null)
            throw new KeyNotFoundException("Product was not found in the shopping cart.");

        await _productBucketRepository.RemoveItemAsync(bucketItem);
    }

    public async Task UpdateProductAsync(Guid userId, Guid productId, UpdateProductBucketItemDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var product = await _productRepository.GetByIdAsync(productId);

        if (product == null)
            throw new KeyNotFoundException("Product not found.");

        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (dto.Quantity > product.StockQuantity)
            throw new InvalidOperationException("Not enough products in stock.");

        var bucket = await _productBucketRepository.GetByUserIdAsync(userId);

        if (bucket == null)
            throw new InvalidOperationException("Shopping cart was not found.");

        var bucketItem = await _productBucketRepository.GetItemAsync(bucket.Id, productId);

        if (bucketItem == null)
            throw new KeyNotFoundException("Product was not found in the shopping cart.");

        bucketItem.Quantity = dto.Quantity;
        bucketItem.UnitPrice = product.Price;
        bucketItem.UpdatedAt = DateTime.UtcNow;

        await _productBucketRepository.UpdateAsync(bucket);
    }
}