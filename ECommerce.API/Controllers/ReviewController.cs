using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _reviewService.GetAllReviewsAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var review = await _reviewService.GetReviewByIdAsync(id);

        if (review == null)
            return NotFound();

        return Ok(review);
    }

    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        return Ok(await _reviewService.GetReviewsByProductIdAsync(productId));
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        return Ok(await _reviewService.GetReviewsByUserIdAsync(userId));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(AddReviewDto dto)
    {
        var userId = GetCurrentUserId();

        var review = await _reviewService.AddReviewAsync(userId, dto);

        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateReviewDto dto)
    {
        await _reviewService.UpdateReviewAsync(id, GetCurrentUserId(), dto);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _reviewService.DeleteReviewAsync(id, GetCurrentUserId());

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }
}
