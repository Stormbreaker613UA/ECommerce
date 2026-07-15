using ECommerce.DAL.DTOs.ProductBucket;
namespace ECommerce.BLL.Services.Interfaces;

public interface IProductBucketService
{
    Task<GetProductBucketDto> GetBucketAsync(Guid userId);

    Task AddProductAsync(Guid userId, AddProductToBucketDto dto);

    Task UpdateProductAsync(Guid userId, Guid productId, UpdateProductBucketItemDto dto);

    Task RemoveProductAsync(Guid userId, Guid productId);

    Task ClearBucketAsync(Guid userId);
}