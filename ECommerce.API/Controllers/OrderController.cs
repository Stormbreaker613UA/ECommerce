using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var orders = await _orderService.GetUserOrdersAsync(
            userId,
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(
            id,
            GetCurrentUserId(),
            cancellationToken);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllForAdmin(
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetAllOrdersAsync(cancellationToken);

        return Ok(orders);
    }

    [HttpGet("admin/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByIdForAdmin(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdForAdminAsync(
            id,
            cancellationToken);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var order = await _orderService.CheckoutAsync(
            userId,
            dto,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            order);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _orderService.CancelOrderAsync(
            GetCurrentUserId(),
            id,
            cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedAccessException(
                "Invalid user token.");

        return userId;
    }
}
