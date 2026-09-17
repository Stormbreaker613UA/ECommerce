using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetAllPaymentsAsync(cancellationToken);
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(
            id,
            GetCurrentUserId(),
            User.IsInRole("Admin"),
            cancellationToken);

        return payment == null ? NotFound() : Ok(payment);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var created = await _paymentService.AddPaymentAsync(
            GetCurrentUserId(),
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        await _paymentService.UpdatePaymentAsync(
            id,
            GetCurrentUserId(),
            User.IsInRole("Admin"),
            request,
            cancellationToken);

        return NoContent();
    }

    // This is an internal/admin completion operation until a trusted provider
    // adapter is added. Gateway response data is deliberately not accepted here.
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompletePaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var completed = await _paymentService.CompletePaymentAsync(
            id,
            GetCurrentUserId(),
            request,
            cancellationToken);

        return Ok(completed);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _paymentService.DeletePaymentAsync(
            id,
            GetCurrentUserId(),
            User.IsInRole("Admin"),
            cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token.");
        }

        return userId;
    }
}
