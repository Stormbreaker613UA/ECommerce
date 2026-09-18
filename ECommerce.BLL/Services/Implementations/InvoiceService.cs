using ECommerce.BLL.Services.Interfaces;
using ECommerce.BLL.Options;
using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.Invoice;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.BLL.Services.Implementations;

public class InvoiceService : IInvoiceService
{
    private const int MaxPageSize = 100;
    private readonly ECommerceDbContext _dbContext;
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceService(
        ECommerceDbContext dbContext,
        IInvoiceRepository invoiceRepository)
    {
        _dbContext = dbContext;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<InvoiceResponseDto> EnsureInvoiceForCompletedPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == paymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");

        if (payment.PaymentStatusId != PaymentStatusCatalog.CompletedId)
            throw new InvalidOperationException("Only a completed payment can issue an invoice.");

        if (string.IsNullOrWhiteSpace(payment.TransactionId))
            throw new InvalidOperationException("Completed payment has no transaction reference.");

        var existing = await _invoiceRepository.GetByOrderIdAsync(
            payment.OrderId,
            cancellationToken);
        if (existing != null)
        {
            EnsureExistingInvoiceMatchesCompletedPayment(existing, payment);
            return MapToDto(existing);
        }

        if (!IsCanonicalCurrency(payment.Currency))
        {
            throw new InvalidOperationException(
                "Completed payment has an invalid currency snapshot.");
        }

        var order = await _dbContext.Orders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == payment.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("Completed payment has no order.");

        if (string.IsNullOrWhiteSpace(order.BillingAddressSnapshot) ||
            !order.BillingAddressId.HasValue)
        {
            // Legacy rows completed before the checkout snapshot migration must
            // not be issued from mutable Address data by guesswork.
            throw new InvalidOperationException(
                "Completed payment cannot be reconciled because its historical billing snapshot is missing.");
        }

        var orderItems = await _dbContext.OrderItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        if (orderItems.Count == 0 || orderItems.Any(item => item.Quantity <= 0))
            throw new InvalidOperationException("Order has no valid line items for invoicing.");

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == order.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Order customer is unavailable for invoicing.");

        if (string.IsNullOrWhiteSpace(user.Email) ||
            string.IsNullOrWhiteSpace(user.FirstName) ||
            string.IsNullOrWhiteSpace(user.LastName))
        {
            throw new InvalidOperationException(
                "Order customer has incomplete billing identity data.");
        }

        var productIds = orderItems.Select(item => item.ProductId).Distinct().ToArray();
        var productNames = await _dbContext.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, product => product.Name, cancellationToken);

        if (productNames.Count != productIds.Length)
            throw new InvalidOperationException("An order product is unavailable for invoicing.");

        var subtotal = orderItems.Sum(item => item.UnitPrice * item.Quantity);
        if (subtotal != order.TotalAmount)
            throw new InvalidOperationException("Order total does not match its historical line items.");

        const decimal taxRate = 0m;
        const decimal taxAmount = 0m;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PaymentId = payment.Id,
            PaymentTransactionId = payment.TransactionId,
            InvoiceNumber = CreateInvoiceNumber(),
            IssuedAt = DateTime.UtcNow,
            Currency = payment.Currency,
            BillingEmail = user.Email,
            BillingFirstName = user.FirstName,
            BillingLastName = user.LastName,
            BillingAddress = order.BillingAddressSnapshot,
            SubtotalAmount = subtotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            TotalAmount = subtotal + taxAmount,
            Items = orderItems
                .OrderBy(item => item.Id)
                .Select(item => new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    ProductName = productNames[item.ProductId],
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.UnitPrice * item.Quantity
                })
                .ToList()
        };

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        return MapToDto(invoice);
    }

    public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(
        Guid invoiceId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var invoice = isAdministrator
            ? await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            : await _invoiceRepository.GetByIdForUserAsync(invoiceId, userId, cancellationToken);

        return invoice == null ? null : MapToDto(invoice);
    }

    public async Task<InvoiceResponseDto?> GetInvoiceByOrderIdAsync(
        Guid orderId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var invoice = isAdministrator
            ? await _invoiceRepository.GetByOrderIdAsync(orderId, cancellationToken)
            : await _invoiceRepository.GetByOrderIdForUserAsync(orderId, userId, cancellationToken);

        return invoice == null ? null : MapToDto(invoice);
    }

    public async Task<InvoicePageResponseDto> GetInvoicesAsync(
        Guid? userId,
        bool isAdministrator,
        InvoicePageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Page < 1)
            throw new ArgumentOutOfRangeException(nameof(request.Page));
        if (request.PageSize is < 1 or > MaxPageSize)
            throw new ArgumentOutOfRangeException(nameof(request.PageSize));
        if (!isAdministrator && !userId.HasValue)
            throw new UnauthorizedAccessException("Invoice owner is required.");

        var (invoices, totalCount) = await _invoiceRepository.GetPageAsync(
            isAdministrator ? null : userId,
            (request.Page - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        return new InvoicePageResponseDto
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            Items = invoices.Select(MapToDto).ToList()
        };
    }

    private static string CreateInvoiceNumber() =>
        $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";

    private static void EnsureExistingInvoiceMatchesCompletedPayment(
        Invoice invoice,
        Payment payment)
    {
        var invoiceLineTotal = invoice.Items.Sum(item => item.LineTotal);
        var validItems = invoice.Items.Count > 0 && invoice.Items.All(item =>
            !string.IsNullOrWhiteSpace(item.ProductName) &&
            item.Quantity > 0 &&
            item.UnitPrice >= 0 &&
            item.LineTotal == item.Quantity * item.UnitPrice);

        if (invoice.OrderId != payment.OrderId ||
            invoice.PaymentId != payment.Id ||
            string.IsNullOrWhiteSpace(invoice.InvoiceNumber) ||
            invoice.IssuedAt == default ||
            !string.Equals(
                invoice.PaymentTransactionId,
                payment.TransactionId,
                StringComparison.Ordinal) ||
            !IsCanonicalCurrency(invoice.Currency) ||
            !string.Equals(invoice.Currency, payment.Currency, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(invoice.BillingEmail) ||
            string.IsNullOrWhiteSpace(invoice.BillingFirstName) ||
            string.IsNullOrWhiteSpace(invoice.BillingLastName) ||
            string.IsNullOrWhiteSpace(invoice.BillingAddress) ||
            invoice.SubtotalAmount < 0 ||
            invoice.TaxRate != 0m ||
            invoice.TaxAmount != 0m ||
            invoice.SubtotalAmount != invoiceLineTotal ||
            invoice.TotalAmount != invoice.SubtotalAmount + invoice.TaxAmount ||
            invoice.TotalAmount != payment.Amount ||
            !validItems)
        {
            throw new InvalidOperationException(
                "Existing invoice cannot be safely replayed for the completed payment.");
        }
    }

    private static bool IsCanonicalCurrency(string? currency)
    {
        try
        {
            return string.Equals(
                CurrencyCode.Normalize(currency),
                currency,
                StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static InvoiceResponseDto MapToDto(Invoice invoice) => new()
    {
        Id = invoice.Id,
        OrderId = invoice.OrderId,
        PaymentId = invoice.PaymentId,
        PaymentTransactionId = invoice.PaymentTransactionId,
        InvoiceNumber = invoice.InvoiceNumber,
        IssuedAt = invoice.IssuedAt,
        Currency = invoice.Currency,
        BillingEmail = invoice.BillingEmail,
        BillingFirstName = invoice.BillingFirstName,
        BillingLastName = invoice.BillingLastName,
        BillingAddress = invoice.BillingAddress,
        SubtotalAmount = invoice.SubtotalAmount,
        TaxRate = invoice.TaxRate,
        TaxAmount = invoice.TaxAmount,
        TotalAmount = invoice.TotalAmount,
        Items = invoice.Items
            .OrderBy(item => item.Id)
            .Select(item => new InvoiceItemResponseDto
            {
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            })
            .ToList()
    };
}
