using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(Guid id);
        Task<Product> AddProductAsync(Product product);
        Task UpdateProductAsync(Guid id, Product product);
        Task DeleteProductAsync(Guid id);
    }
}