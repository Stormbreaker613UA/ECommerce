using Microsoft.AspNetCore.Mvc;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductBucketController : ControllerBase
    {
        private readonly IProductBucketService _productBucketService;

        public ProductBucketController(IProductBucketService productBucketService)
        {
            _productBucketService = productBucketService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            var bucket = await _productBucketService.GetByUserIdAsync(userId);
            if (bucket == null) return NotFound();
            return Ok(bucket);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductBucket productBucket)
        {
            await _productBucketService.AddProductBucketAsync(productBucket);
            return CreatedAtAction(nameof(GetByUser), new { userId = productBucket.UserId }, productBucket);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductBucket productBucket)
        {
            productBucket.Id = id;
            await _productBucketService.UpdateProductBucketAsync(productBucket);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _productBucketService.DeleteProductBucketAsync(id);
            return NoContent();
        }
    }
}