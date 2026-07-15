using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.ProductBucket;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductBucketController : ControllerBase
{
    private readonly IProductBucketService _productBucketService;

    public ProductBucketController(IProductBucketService productBucketService)
    {
        _productBucketService = productBucketService;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetBucket(Guid userId)
    {
        var bucket = await _productBucketService.GetBucketAsync(userId);
        return Ok(bucket);
    }

    [HttpPost("{userId:guid}/items")]
    public async Task<IActionResult> AddProduct(
        Guid userId,
        [FromBody] AddProductToBucketDto dto)
    {
        await _productBucketService.AddProductAsync(userId, dto);
        return Ok();
    }

    [HttpPut("{userId:guid}/items/{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid userId,
        Guid productId,
        [FromBody] UpdateProductBucketItemDto dto)
    {
        await _productBucketService.UpdateProductAsync(userId, productId, dto);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/items/{productId:guid}")]
    public async Task<IActionResult> RemoveProduct(
        Guid userId,
        Guid productId)
    {
        await _productBucketService.RemoveProductAsync(userId, productId);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> ClearBucket(Guid userId)
    {
        await _productBucketService.ClearBucketAsync(userId);
        return NoContent();
    }
}