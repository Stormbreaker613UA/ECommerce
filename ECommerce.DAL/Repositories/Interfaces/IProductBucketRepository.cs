using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.DAL.Repositories.Interfaces
{
    public interface IProductBucketRepository
    {
        Task<ProductBucket?> GetByUserIdAsync(Guid userId);
        Task AddAsync(ProductBucket bucket);
        Task UpdateAsync(ProductBucket bucket);
        Task DeleteAsync(Guid id);
    }
}
