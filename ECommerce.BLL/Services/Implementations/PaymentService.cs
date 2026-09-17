using ECommerce.BLL.Services.Interfaces;
using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.Payment;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ECommerce.BLL.Services.Implementations;

public class PaymentService : IPaymentService
{
    private const string SupportedCurrency = "USD";
    private const int MaxIdempotencyKeyLength = 200;
    private const string CreationIdempotencyIndex =
        "IX_Payments_IdempotencyKey";
    private const string CompletionIdempotencyIndex =
        "IX_Payments_CompletionIdempotencyKey";

    private readonly ECommerceDbContext _dbContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ECommerceDbContext dbContext,
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<List<PaymentResponseDto>> GetAllPaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetAllAsync(cancellationToken);
        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaymentResponseDto?> GetPaymentByIdAsync(
        Guid paymentId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var payment = isAdministrator
            ? await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
            : await _paymentRepository.GetByIdForUserAsync(
                paymentId,
                userId,
                cancellationToken);

        return payment == null ? null : MapToDto(payment);
    }

    public async Task<PaymentResponseDto> AddPaymentAsync(
        Guid userId,
        CreatePaymentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        var currency = NormalizeCurrency(request.Currency);
        ValidateCreateInput(request, idempotencyKey);

        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            if (!await _orderRepository.LockOrderRowAsync(
                    request.OrderId,
                    cancellationToken))
            {
                throw new KeyNotFoundException("Order not found.");
            }

            var existingByKey = await _paymentRepository
                .GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);

            if (existingByKey != null)
            {
                EnsureCreateReplayMatches(
                    existingByKey,
                    userId,
                    request,
                    currency);

                await transaction.CommitAsync(cancellationToken);
                return MapToDto(existingByKey);
            }

            var order = await _orderRepository.GetByIdAsync(
                request.OrderId,
                cancellationToken);

            if (order == null || order.UserId != userId)
                throw new KeyNotFoundException("Order not found.");

            EnsureOrderEligibleForPayment(order);
            ValidateAmount(request.Amount, order.TotalAmount);

            var paymentMethod = await _dbContext.PaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    method => method.Id == request.PaymentMethodId,
                    cancellationToken);

            if (paymentMethod == null)
                throw new KeyNotFoundException("Payment method not found.");

            var activePayment = await _paymentRepository.GetByOrderIdAsync(
                request.OrderId,
                cancellationToken);

            if (activePayment != null)
            {
                if (activePayment.IdempotencyKey == idempotencyKey)
                {
                    EnsureCreateReplayMatches(
                        activePayment,
                        userId,
                        request,
                        currency);

                    await transaction.CommitAsync(cancellationToken);
                    return MapToDto(activePayment);
                }

                throw new InvalidOperationException(
                    "An active payment already exists for this order.");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentStatusId = PaymentStatusCatalog.PendingId,
                PaymentMethodId = paymentMethod.Id,
                Amount = order.TotalAmount,
                Currency = currency,
                IdempotencyKey = idempotencyKey,
                CreatedBy = userId
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);

            var createdPayment = await _paymentRepository.GetByIdAsync(
                payment.Id,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "The payment could not be reloaded after creation.");

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Payment creation succeeded for user {UserId}, order {OrderId}, payment {PaymentId}.",
                userId,
                order.Id,
                payment.Id);

            return MapToDto(createdPayment);
        }
        catch (Exception ex) when (IsPostgresUniqueConstraintViolation(
            ex,
            CreationIdempotencyIndex))
        {
            await RollbackAsync(transaction, userId, request.OrderId);
            _dbContext.ChangeTracker.Clear();

            // A concurrent request with the same idempotency key may have won
            // the unique index after this transaction started. Return its
            // persisted result instead of turning a safe retry into a 500.
            var persistedReplay = await _paymentRepository
                .GetByIdempotencyKeyAsync(idempotencyKey, CancellationToken.None);

            if (persistedReplay != null)
            {
                EnsureCreateReplayMatches(
                    persistedReplay,
                    userId,
                    request,
                    currency);

                return MapToDto(persistedReplay);
            }

            _logger.LogError(
                ex,
                "Payment creation hit its idempotency unique constraint but no persisted replay was found for user {UserId} and order {OrderId}.",
                userId,
                request.OrderId);

            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollbackAsync(transaction, userId, request.OrderId);
            _logger.LogDebug(
                "Payment creation was cancelled for user {UserId} and order {OrderId}.",
                userId,
                request.OrderId);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            await RollbackAsync(transaction, userId, request.OrderId);
            _logger.LogWarning(
                ex,
                "Payment creation timed out for user {UserId} and order {OrderId}.",
                userId,
                request.OrderId);
            throw;
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction, userId, request.OrderId);
            _logger.LogWarning(
                ex,
                "Payment creation failed for user {UserId} and order {OrderId}.",
                userId,
                request.OrderId);
            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    public async Task UpdatePaymentAsync(
        Guid paymentId,
        Guid userId,
        bool isAdministrator,
        UpdatePaymentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payment = await GetForActorAsync(
            paymentId,
            userId,
            isAdministrator,
            cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");

        EnsurePending(payment);

        var paymentMethodExists = await _dbContext.PaymentMethods
            .AsNoTracking()
            .AnyAsync(
                method => method.Id == request.PaymentMethodId,
                cancellationToken);

        if (!paymentMethodExists)
            throw new KeyNotFoundException("Payment method not found.");

        var updated = await _paymentRepository.TryUpdatePaymentMethodAsync(
            paymentId,
            PaymentStatusCatalog.PendingId,
            request.PaymentMethodId,
            DateTime.UtcNow,
            userId,
            cancellationToken);

        if (updated != 1)
            throw new InvalidOperationException(
                "The payment status changed before the payment method could be updated.");

        _logger.LogInformation(
            "Payment method updated for user {UserId}, payment {PaymentId}.",
            userId,
            paymentId);
    }

    public async Task<PaymentResponseDto> CompletePaymentAsync(
        Guid paymentId,
        Guid actorId,
        CompletePaymentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            var paymentForLock = await _paymentRepository.GetByIdAsync(
                paymentId,
                cancellationToken)
                ?? throw new KeyNotFoundException("Payment not found.");

            if (!await _orderRepository.LockOrderRowAsync(
                    paymentForLock.OrderId,
                    cancellationToken))
            {
                throw new KeyNotFoundException("Order not found.");
            }

            var existingByKey = await _paymentRepository
                .GetByCompletionIdempotencyKeyAsync(idempotencyKey, cancellationToken);

            if (existingByKey != null)
            {
                if (existingByKey.Id != paymentId)
                {
                    throw new InvalidOperationException(
                        "The completion idempotency key was already used for another payment.");
                }

                await transaction.CommitAsync(cancellationToken);
                return MapToDto(existingByKey);
            }

            var payment = await _paymentRepository.GetByIdAsync(
                paymentId,
                cancellationToken)
                ?? throw new KeyNotFoundException("Payment not found.");

            if (payment.PaymentStatusId == PaymentStatusCatalog.CompletedId)
            {
                throw new InvalidOperationException(
                    "The payment has already been completed.");
            }

            if (payment.PaymentStatusId == PaymentStatusCatalog.FailedId)
            {
                throw new InvalidOperationException(
                    "A failed payment attempt cannot be completed.");
            }

            EnsureOrderEligibleForPayment(payment.Order);

            var paidAt = DateTime.UtcNow;
            var transactionId = $"internal-{Guid.NewGuid():N}";
            var completed = await _paymentRepository.TryCompleteAsync(
                paymentId,
                PaymentStatusCatalog.PendingId,
                PaymentStatusCatalog.CompletedId,
                idempotencyKey,
                transactionId,
                paidAt,
                actorId,
                cancellationToken);

            if (completed != 1)
            {
                var current = await _paymentRepository.GetByIdAsync(
                    paymentId,
                    cancellationToken)
                    ?? throw new KeyNotFoundException("Payment not found.");

                if (current.PaymentStatusId == PaymentStatusCatalog.CompletedId &&
                    current.CompletionIdempotencyKey == idempotencyKey)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return MapToDto(current);
                }

                throw new InvalidOperationException(
                    "The payment status changed before completion could be recorded.");
            }

            var completedPayment = await _paymentRepository.GetByIdAsync(
                paymentId,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    "The completed payment could not be reloaded.");

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Payment completion succeeded for payment {PaymentId} and order {OrderId}.",
                paymentId,
                payment.OrderId);

            return MapToDto(completedPayment);
        }
        catch (Exception ex) when (IsPostgresUniqueConstraintViolation(
            ex,
            CompletionIdempotencyIndex))
        {
            await RollbackAsync(transaction, actorId, paymentId);
            _dbContext.ChangeTracker.Clear();

            var persistedCompletion = await _paymentRepository
                .GetByCompletionIdempotencyKeyAsync(
                    idempotencyKey,
                    CancellationToken.None);

            if (persistedCompletion != null)
            {
                if (persistedCompletion.Id == paymentId)
                {
                    _logger.LogInformation(
                        "Payment completion replayed after a completion idempotency race for payment {PaymentId}.",
                        paymentId);

                    return MapToDto(persistedCompletion);
                }

                throw new InvalidOperationException(
                    "The completion idempotency key was already used for another payment.",
                    ex);
            }

            _logger.LogError(
                ex,
                "Payment completion hit its idempotency unique constraint but no persisted owner was found for payment {PaymentId}.",
                paymentId);

            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RollbackAsync(transaction, actorId, paymentId);
            _logger.LogDebug(
                "Payment completion was cancelled for payment {PaymentId}.",
                paymentId);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            await RollbackAsync(transaction, actorId, paymentId);
            _logger.LogWarning(
                ex,
                "Payment completion timed out for payment {PaymentId}.",
                paymentId);
            throw;
        }
        catch (Exception ex)
        {
            await RollbackAsync(transaction, actorId, paymentId);
            _logger.LogWarning(
                ex,
                "Payment completion failed for payment {PaymentId}.",
                paymentId);
            throw;
        }
        finally
        {
            if (transaction != null)
                await transaction.DisposeAsync();
        }
    }

    public async Task DeletePaymentAsync(
        Guid paymentId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var payment = await GetForActorAsync(
            paymentId,
            userId,
            isAdministrator,
            cancellationToken)
            ?? throw new KeyNotFoundException("Payment not found.");

        if (payment.PaymentStatusId == PaymentStatusCatalog.CompletedId)
        {
            throw new InvalidOperationException(
                "Completed payments cannot be deleted; use a refund workflow.");
        }

        if (payment.PaymentStatusId == PaymentStatusCatalog.FailedId)
            return;

        EnsurePending(payment);

        var updated = await _paymentRepository.TryMarkFailedAsync(
            paymentId,
            PaymentStatusCatalog.PendingId,
            PaymentStatusCatalog.FailedId,
            DateTime.UtcNow,
            userId,
            cancellationToken);

        if (updated != 1)
        {
            var current = await GetForActorAsync(
                paymentId,
                userId,
                isAdministrator,
                cancellationToken)
                ?? throw new KeyNotFoundException("Payment not found.");

            if (current.PaymentStatusId == PaymentStatusCatalog.FailedId)
                return;

            if (current.PaymentStatusId == PaymentStatusCatalog.CompletedId)
            {
                throw new InvalidOperationException(
                    "Completed payments cannot be deleted; use a refund workflow.");
            }

            throw new InvalidOperationException(
                "The payment status changed before it could be failed.");
        }

        _logger.LogInformation(
            "Payment {PaymentId} was marked Failed by user {UserId}; financial history was retained.",
            paymentId,
            userId);
    }

    private async Task<Payment?> GetForActorAsync(
        Guid paymentId,
        Guid userId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        return isAdministrator
            ? await _paymentRepository.GetByIdAsync(paymentId, cancellationToken)
            : await _paymentRepository.GetByIdForUserAsync(
                paymentId,
                userId,
                cancellationToken);
    }

    private static void ValidateCreateInput(
        CreatePaymentRequestDto request,
        string idempotencyKey)
    {
        if (request.OrderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.");

        if (request.PaymentMethodId == Guid.Empty)
            throw new ArgumentException("PaymentMethodId is required.");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("IdempotencyKey is required.");
    }

    private static string NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IdempotencyKey is required.");

        var normalized = value.Trim();

        if (normalized.Length > MaxIdempotencyKeyLength)
        {
            throw new ArgumentException(
                $"IdempotencyKey must be {MaxIdempotencyKeyLength} characters or fewer.");
        }

        return normalized;
    }

    private static string NormalizeCurrency(string? value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!string.Equals(normalized, SupportedCurrency, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Currency must be {SupportedCurrency} for the current payment foundation.");
        }

        return normalized;
    }

    private static void ValidateAmount(decimal requestedAmount, decimal orderAmount)
    {
        if (requestedAmount <= 0 || requestedAmount != decimal.Round(requestedAmount, 2))
            throw new ArgumentException("Amount must be positive and have at most two decimal places.");

        if (orderAmount <= 0)
            throw new InvalidOperationException("The order has no payable amount.");

        if (requestedAmount != orderAmount)
        {
            throw new ArgumentException(
                "Amount must match the authenticated order total.");
        }
    }

    private static void EnsureOrderEligibleForPayment(Order? order)
    {
        if (order == null)
            throw new KeyNotFoundException("Order not found.");

        var status = order.OrderStatus?.Name;
        if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The order is not eligible for payment in its current status.");
        }
    }

    private static void EnsurePending(Payment payment)
    {
        if (payment.PaymentStatusId != PaymentStatusCatalog.PendingId)
        {
            var status = payment.PaymentStatus?.Status ?? "unknown";
            throw new InvalidOperationException(
                $"Payment method changes are allowed only while the payment is Pending; current status is {status}.");
        }
    }

    private static void EnsureCreateReplayMatches(
        Payment payment,
        Guid userId,
        CreatePaymentRequestDto request,
        string currency)
    {
        if (payment.Order?.UserId != userId)
            throw new KeyNotFoundException("Payment not found.");

        if (payment.OrderId != request.OrderId ||
            payment.PaymentMethodId != request.PaymentMethodId ||
            payment.Amount != request.Amount ||
            !string.Equals(payment.Currency, currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The idempotency key was already used with different payment data.");
        }
    }

    private static PaymentResponseDto MapToDto(Payment payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod?.Method ?? string.Empty,
            Status = payment.PaymentStatus?.Status ?? ResolveStatusName(payment.PaymentStatusId),
            TransactionId = payment.TransactionId,
            CardLast4 = payment.CardLast4,
            CardBrand = payment.CardBrand,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    private static string ResolveStatusName(Guid statusId)
    {
        if (statusId == PaymentStatusCatalog.PendingId)
            return PaymentStatusCatalog.Pending;
        if (statusId == PaymentStatusCatalog.CompletedId)
            return PaymentStatusCatalog.Completed;
        if (statusId == PaymentStatusCatalog.FailedId)
            return PaymentStatusCatalog.Failed;

        return string.Empty;
    }

    private static bool IsPostgresUniqueConstraintViolation(
        Exception exception,
        string constraintName)
    {
        for (var current = exception;
             current != null;
             current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                string.Equals(
                    postgresException.ConstraintName,
                    constraintName,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private async Task RollbackAsync(
        IDbContextTransaction? transaction,
        Guid actorId,
        Guid resourceId)
    {
        if (transaction == null)
            return;

        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(
                rollbackException,
                "Payment transaction rollback failed for actor {ActorId} and resource {ResourceId}.",
                actorId,
                resourceId);
        }
    }
}
