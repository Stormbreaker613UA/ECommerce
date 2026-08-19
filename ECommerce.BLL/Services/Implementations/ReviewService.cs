using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Review;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;

namespace ECommerce.BLL.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;

    public ReviewService(
        IReviewRepository reviewRepository,
        IUserRepository userRepository,
        IProductRepository productRepository)
    {
        _reviewRepository = reviewRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
    }

    public async Task<List<GetReviewDto>> GetAllReviewsAsync()
    {
        var reviews = await _reviewRepository.GetAllAsync();

        return reviews.Select(MapToDto).ToList();
    }

    public async Task<GetReviewDto?> GetReviewByIdAsync(Guid id)
    {
        var review = await _reviewRepository.GetByIdAsync(id);

        return review == null
            ? null
            : MapToDto(review);
    }

    public async Task<List<GetReviewDto>> GetReviewsByProductIdAsync(Guid productId)
    {
        var reviews = await _reviewRepository.GetByProductIdAsync(productId);

        return reviews.Select(MapToDto).ToList();
    }

    public async Task<List<GetReviewDto>> GetReviewsByUserIdAsync(Guid userId)
    {
        var reviews = await _reviewRepository.GetByUserIdAsync(userId);

        return reviews.Select(MapToDto).ToList();
    }

    public async Task<GetReviewDto> AddReviewAsync(
        Guid userId,
        AddReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var product = await _productRepository.GetByIdAsync(dto.ProductId);

        if (product == null)
            throw new KeyNotFoundException("Product not found.");

        var existingReview =
            await _reviewRepository.GetByUserAndProductAsync(
                userId,
                dto.ProductId);

        if (existingReview != null)
            throw new InvalidOperationException(
                "You already reviewed this product.");

        var review = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = dto.ProductId,
            Rating = dto.Rating,
            CommentText = dto.CommentText ?? string.Empty
        };

        await _reviewRepository.AddAsync(review);

        review.User = user;

        return MapToDto(review);
    }

    public async Task UpdateReviewAsync(
        Guid reviewId,
        Guid userId,
        UpdateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var review = await _reviewRepository.GetByIdAsync(reviewId);

        if (review == null)
            throw new KeyNotFoundException("Review not found.");

        if (review.UserId != userId)
            throw new UnauthorizedAccessException();

        review.Rating = dto.Rating;
        review.CommentText = dto.CommentText ?? string.Empty;

        await _reviewRepository.UpdateAsync(review);
    }

    public async Task DeleteReviewAsync(
        Guid reviewId,
        Guid userId)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId);

        if (review == null)
            throw new KeyNotFoundException("Review not found.");

        if (review.UserId != userId)
            throw new UnauthorizedAccessException();

        await _reviewRepository.DeleteAsync(reviewId);
    }

    private static GetReviewDto MapToDto(Review review)
    {
        return new GetReviewDto
        {
            Id = review.Id,
            UserId = review.UserId,
            UserName = review.User is null
                ? string.Empty
                : $"{review.User.FirstName} {review.User.LastName}".Trim(),
            ProductId = review.ProductId,
            Rating = review.Rating,
            CommentText = review.CommentText,
            CreatedAt = review.CreatedAt
        };
    }
}