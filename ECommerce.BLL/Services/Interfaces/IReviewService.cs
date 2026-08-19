using ECommerce.DAL.DTOs.Review;

namespace ECommerce.BLL.Services.Interfaces;


public interface IReviewService
{
    Task<List<GetReviewDto>> GetAllReviewsAsync();

    Task<GetReviewDto?> GetReviewByIdAsync(Guid id);

    Task<List<GetReviewDto>> GetReviewsByProductIdAsync(Guid productId);

    Task<List<GetReviewDto>> GetReviewsByUserIdAsync(Guid userId);

    Task<GetReviewDto> AddReviewAsync(Guid userId, AddReviewDto dto);

    Task UpdateReviewAsync(Guid reviewId, Guid userId, UpdateReviewDto dto);

    Task DeleteReviewAsync(Guid reviewId, Guid userId);
}
