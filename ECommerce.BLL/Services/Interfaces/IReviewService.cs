using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;

namespace ECommerce.BLL.Services.Interfaces
{
    public interface IReviewService
    {
        Task<List<Review>> GetAllReviewsAsync();
        Task<Review?> GetReviewByIdAsync(Guid id);
        Task<List<Review>> GetReviewsByProductIdAsync(Guid productId);
        Task<List<Review>> GetReviewsByUserIdAsync(Guid userId);
        Task<Review> AddReviewAsync(Review review);
        Task UpdateReviewAsync(Guid id, Review review);
        Task DeleteReviewAsync(Guid id);
    }
}
