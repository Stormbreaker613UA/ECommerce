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
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetCurrentUserId();

        var orders =
            await _orderService.GetUserOrdersAsync(userId);

        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order =
            await _orderService.GetOrderByIdAsync(id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        var userId = GetCurrentUserId();

        var order = await _orderService.CheckoutAsync(userId, dto);

        return Ok(order);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _orderService.CancelOrderAsync(GetCurrentUserId(), id);

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