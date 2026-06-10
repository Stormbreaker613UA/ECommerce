using System;
using System.Threading.Tasks;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations
{
    public class ProductBucketService : IProductBucketService
    {
        private readonly IProductBucketRepository _productBucketRepository;

        public ProductBucketService(IProductBucketRepository productBucketRepository)
        {
            _productBucketRepository = productBucketRepository;
        }

        public async Task<ProductBucket?> GetByUserIdAsync(Guid userId)
        {
            return await _productBucketRepository.GetByUserIdAsync(userId);
        }

        public async Task AddProductBucketAsync(ProductBucket productBucket)
        {
            await _productBucketRepository.AddAsync(productBucket);
        }

        public async Task UpdateProductBucketAsync(ProductBucket productBucket)
        {
            await _productBucketRepository.UpdateAsync(productBucket);
        }

        public async Task DeleteProductBucketAsync(Guid id)
        {
            await _productBucketRepository.DeleteAsync(id);
        }
    }
}