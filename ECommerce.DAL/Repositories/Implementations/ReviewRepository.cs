using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.DAL.Entities;
using ECommerce.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.DAL.Repositories.Implementations
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ECommerceDbContext _dbContext;

        public ReviewRepository(ECommerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Review>> GetAllAsync()
        {
            return await _dbContext.Reviews.ToListAsync();
        }

        public async Task<Review?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Reviews.FindAsync(id);
        }

        public async Task<List<Review>> GetByProductIdAsync(Guid productId)
        {
            return await _dbContext.Reviews.Where(r => r.ProductId == productId).ToListAsync();
        }

        public async Task<List<Review>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Reviews.Where(r => r.UserId == userId).ToListAsync();
        }

        public async Task AddAsync(Review review)
        {
            await _dbContext.Reviews.AddAsync(review);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Review review)
        {
            _dbContext.Reviews.Update(review);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var review = await _dbContext.Reviews.FindAsync(id);
            if (review != null)
            {
                _dbContext.Reviews.Remove(review);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
