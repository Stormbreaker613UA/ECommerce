using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IProductBucketService
    {
        Task<ProductBucket?> GetByUserIdAsync(Guid userId);
        Task AddProductBucketAsync(ProductBucket productBucket);
        Task UpdateProductBucketAsync(ProductBucket productBucket);
        Task DeleteProductBucketAsync(Guid id);
    }
}