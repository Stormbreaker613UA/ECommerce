using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewService(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public async Task<List<Review>> GetAllReviewsAsync()
        {
            return await _reviewRepository.GetAllAsync();
        }

        public async Task<Review?> GetReviewByIdAsync(Guid id)
        {
            return await _reviewRepository.GetByIdAsync(id);
        }

        public async Task<List<Review>> GetReviewsByProductIdAsync(Guid productId)
        {
            return await _reviewRepository.GetByProductIdAsync(productId);
        }

        public async Task<List<Review>> GetReviewsByUserIdAsync(Guid userId)
        {
            return await _reviewRepository.GetByUserIdAsync(userId);
        }

        public async Task<Review> AddReviewAsync(Review review)
        {
            await _reviewRepository.AddAsync(review);
            return review;
        }

        public async Task UpdateReviewAsync(Guid id, Review review)
        {
            var existing = await _reviewRepository.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException("Review not found");

            existing.Rating = review.Rating;
            existing.CommentText = review.CommentText;

            await _reviewRepository.UpdateAsync(existing);
        }

        public async Task DeleteReviewAsync(Guid id)
        {
            await _reviewRepository.DeleteAsync(id);
        }
    }
}
