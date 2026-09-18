using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DTOs.Invoice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<ActionResult<InvoicePageResponseDto>> GetMine(
        [FromQuery] InvoicePageRequestDto request,
        CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetInvoicesAsync(
            GetCurrentUserId(),
            isAdministrator: false,
            request,
            cancellationToken);
        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceResponseDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(
            id,
            GetCurrentUserId(),
            User.IsInRole("Admin"),
            cancellationToken);

        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<InvoiceResponseDto>> GetByOrderId(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(
            orderId,
            GetCurrentUserId(),
            User.IsInRole("Admin"),
            cancellationToken);

        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<InvoicePageResponseDto>> GetAll(
        [FromQuery] InvoicePageRequestDto request,
        CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetInvoicesAsync(
            userId: null,
            isAdministrator: true,
            request,
            cancellationToken);
        return Ok(invoices);
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new UnauthorizedAccessException("Invalid user token.");

        return userId;
    }
}
