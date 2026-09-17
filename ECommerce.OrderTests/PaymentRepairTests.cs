using ECommerce.BLL.Services.Implementations;
using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.Payment;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.OrderTests;

[Collection(PostgreSqlOrderCollection.Name)]
public sealed class PaymentRepairTests
{
    private static readonly Guid CardPaymentMethodId =
        new("d1b2c3d4-0000-0000-0000-000000000001");

    private readonly PostgreSqlOrderFixture _fixture;

    public PaymentRepairTests(PostgreSqlOrderFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Empty_database_migrations_seed_payment_reference_data()
    {
        await _fixture.ResetAsync();

        await using var context = _fixture.CreateContext();

        var statuses = await context.PaymentStatuses
            .AsNoTracking()
            .ToDictionaryAsync(status => status.Status);
        var methods = await context.PaymentMethods
            .AsNoTracking()
            .ToDictionaryAsync(method => method.Method);

        Assert.Equal(PaymentStatusCatalog.PendingId, statuses[PaymentStatusCatalog.Pending].Id);
        Assert.Equal(PaymentStatusCatalog.CompletedId, statuses[PaymentStatusCatalog.Completed].Id);
        Assert.Equal(PaymentStatusCatalog.FailedId, statuses[PaymentStatusCatalog.Failed].Id);
        Assert.Equal(CardPaymentMethodId, methods["Card"].Id);
        Assert.Equal(
            new Guid("d1b2c3d4-0000-0000-0000-000000000002"),
            methods["Cash"].Id);
        Assert.Equal(
            new Guid("d1b2c3d4-0000-0000-0000-000000000003"),
            methods["BankTransfer"].Id);
    }

    [Fact]
    public async Task Completion_and_cancellation_are_serialized_by_the_order_row_lock()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "completion-race-create");

        await using var blocker = _fixture.CreateContext();
        await using var blockerTransaction =
            await blocker.Database.BeginTransactionAsync();

        Assert.True(await new OrderRepository(blocker).LockOrderRowAsync(scenario.OrderId));

        var completionTask = CompleteWithNewContextAsync(
            scenario,
            payment.Id,
            "completion-race-a");
        var cancellationTask = CancelWithNewContextAsync(scenario);

        await AssertTasksBlockedByOrderLockAsync(completionTask, cancellationTask);

        await blockerTransaction.CommitAsync(CancellationToken.None);

        var results = await Task.WhenAll(completionTask, cancellationTask);
        Assert.True(
            results.Count(result => result == null) == 1,
            string.Join(
                Environment.NewLine,
                results.Select(result => result?.ToString() ?? "<success>")));
        Assert.Equal(1, results.Count(result => result is InvalidOperationException));

        await using var verifyContext = _fixture.CreateContext();
        var order = await verifyContext.Orders
            .Include(item => item.OrderStatus)
            .SingleAsync(item => item.Id == scenario.OrderId);
        var persistedPayment = await verifyContext.Payments
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == payment.Id);

        Assert.False(
            order.OrderStatus.Name == "Cancelled" &&
            persistedPayment.PaymentStatusId == PaymentStatusCatalog.CompletedId);

        if (persistedPayment.PaymentStatusId == PaymentStatusCatalog.CompletedId)
            Assert.Equal("Pending", order.OrderStatus.Name);
        else
            Assert.Equal(PaymentStatusCatalog.FailedId, persistedPayment.PaymentStatusId);
    }

    [Fact]
    public async Task Payment_creation_and_cancellation_are_serialized_by_the_order_row_lock()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();

        await using var blocker = _fixture.CreateContext();
        await using var blockerTransaction =
            await blocker.Database.BeginTransactionAsync();

        Assert.True(await new OrderRepository(blocker).LockOrderRowAsync(scenario.OrderId));

        var createTask = CreateWithNewContextAsync(
            scenario,
            "creation-race");
        var cancellationTask = CancelWithNewContextAsync(scenario);

        await AssertTasksBlockedByOrderLockAsync(createTask, cancellationTask);

        await blockerTransaction.CommitAsync(CancellationToken.None);

        var results = await Task.WhenAll(createTask, cancellationTask);
        Assert.Null(results[1]);
        Assert.True(
            results[0] == null || results[0] is InvalidOperationException,
            results[0]?.ToString() ?? "<success>");

        await using var verifyContext = _fixture.CreateContext();
        var order = await verifyContext.Orders
            .Include(item => item.OrderStatus)
            .SingleAsync(item => item.Id == scenario.OrderId);
        var payments = await verifyContext.Payments
            .IgnoreQueryFilters()
            .Where(item => item.OrderId == scenario.OrderId)
            .ToListAsync();

        Assert.False(
            order.OrderStatus.Name == "Cancelled" &&
            payments.Any(item => item.PaymentStatusId == PaymentStatusCatalog.PendingId ||
                                 item.PaymentStatusId == PaymentStatusCatalog.CompletedId));
    }

    [Fact]
    public async Task Repeating_completion_with_the_same_idempotency_key_replays_the_same_payment()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "completion-replay-create");

        await using var context = _fixture.CreateContext();
        var service = CreatePaymentService(context);
        var request = new CompletePaymentRequestDto
        {
            IdempotencyKey = "completion-replay"
        };

        var first = await service.CompletePaymentAsync(
            payment.Id,
            scenario.UserId,
            request);
        var second = await service.CompletePaymentAsync(
            payment.Id,
            scenario.UserId,
            request);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.TransactionId, second.TransactionId);
        Assert.Equal(PaymentStatusCatalog.Completed, second.Status);

        await using var verifyContext = _fixture.CreateContext();
        Assert.Equal(
            1,
            await verifyContext.Payments.CountAsync(item => item.Id == payment.Id));
        Assert.Equal(
            1,
            await verifyContext.Payments.CountAsync(
                item => item.CompletionIdempotencyKey == "completion-replay"));
    }

    [Fact]
    public async Task Competing_completion_keys_can_complete_a_payment_only_once()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "completion-compete-create");

        await using var blocker = _fixture.CreateContext();
        await using var blockerTransaction =
            await blocker.Database.BeginTransactionAsync();

        Assert.True(await new OrderRepository(blocker).LockOrderRowAsync(scenario.OrderId));

        var firstTask = CompleteWithNewContextAsync(
            scenario,
            payment.Id,
            "completion-compete-a");
        var secondTask = CompleteWithNewContextAsync(
            scenario,
            payment.Id,
            "completion-compete-b");

        await AssertTasksBlockedByOrderLockAsync(firstTask, secondTask);

        await blockerTransaction.CommitAsync(CancellationToken.None);

        var results = await Task.WhenAll(firstTask, secondTask);
        Assert.Single(results, result => result == null);
        Assert.Single(
            results,
            result => result is InvalidOperationException);

        await using var verifyContext = _fixture.CreateContext();
        var persistedPayment = await verifyContext.Payments
            .SingleAsync(item => item.Id == payment.Id);

        Assert.Equal(PaymentStatusCatalog.CompletedId, persistedPayment.PaymentStatusId);
        Assert.Contains(
            persistedPayment.CompletionIdempotencyKey,
            new[] { "completion-compete-a", "completion-compete-b" });
        Assert.Equal(1, await verifyContext.Payments.CountAsync(
            item => item.CompletionIdempotencyKey != null));
    }

    [Fact]
    public async Task Same_completion_key_on_different_payments_returns_a_conflict_not_a_database_error()
    {
        await _fixture.ResetAsync();
        var firstScenario = await SeedCheckedOutOrderAsync();
        var secondScenario = await SeedCheckedOutOrderAsync();
        var firstPayment = await CreatePaymentAsync(
            firstScenario,
            "same-completion-key-create-a");
        var secondPayment = await CreatePaymentAsync(
            secondScenario,
            "same-completion-key-create-b");

        await using var blocker = _fixture.CreateContext();
        await using var blockerTransaction =
            await blocker.Database.BeginTransactionAsync();
        var orderRepository = new OrderRepository(blocker);

        Assert.True(await orderRepository.LockOrderRowAsync(firstScenario.OrderId));
        Assert.True(await orderRepository.LockOrderRowAsync(secondScenario.OrderId));

        const string completionKey = "same-completion-key";
        var firstTask = CompleteWithNewContextAsync(
            firstScenario,
            firstPayment.Id,
            completionKey);
        var secondTask = CompleteWithNewContextAsync(
            secondScenario,
            secondPayment.Id,
            completionKey);

        await AssertTasksBlockedByOrderLockAsync(firstTask, secondTask);

        await blockerTransaction.CommitAsync(CancellationToken.None);

        var results = await Task.WhenAll(firstTask, secondTask);
        Assert.Single(results, result => result == null);
        Assert.Single(results, result => result is InvalidOperationException);
        Assert.All(
            results,
            result => Assert.True(
                result == null || result is InvalidOperationException,
                result?.GetType().FullName ?? "<success>"));

        await using var verifyContext = _fixture.CreateContext();
        var owners = await verifyContext.Payments
            .Where(payment => payment.CompletionIdempotencyKey == completionKey)
            .ToListAsync();

        var owner = Assert.Single(owners);
        Assert.Equal(PaymentStatusCatalog.CompletedId, owner.PaymentStatusId);
        Assert.Contains(owner.Id, new[] { firstPayment.Id, secondPayment.Id });
    }

    private async Task<PaymentScenario> SeedCheckedOutOrderAsync()
    {
        await using var context = _fixture.CreateContext();
        var orderScenario = await OrderTestData.SeedAsync(
            context,
            initialStock: 10,
            quantity: 2);
        var order = await OrderServiceFactory.Create(context)
            .CheckoutAsync(
                orderScenario.UserId,
                new ECommerce.DAL.DTOs.Order.CheckoutDto
                {
                    AddressId = orderScenario.AddressId
                });

        return new PaymentScenario(
            orderScenario,
            order.Id,
            order.TotalAmount);
    }

    private async Task<PaymentResponseDto> CreatePaymentAsync(
        PaymentScenario scenario,
        string idempotencyKey)
    {
        await using var context = _fixture.CreateContext();
        return await CreatePaymentService(context).AddPaymentAsync(
            scenario.UserId,
            new CreatePaymentRequestDto
            {
                OrderId = scenario.OrderId,
                PaymentMethodId = CardPaymentMethodId,
                Amount = scenario.Amount,
                Currency = "USD",
                IdempotencyKey = idempotencyKey
            });
    }

    private async Task<Exception?> CreateWithNewContextAsync(
        PaymentScenario scenario,
        string idempotencyKey)
    {
        await using var context = _fixture.CreateContext();
        return await AsyncTestCapture.CaptureAsync(async () =>
        {
            await CreatePaymentService(context).AddPaymentAsync(
                scenario.UserId,
                new CreatePaymentRequestDto
                {
                    OrderId = scenario.OrderId,
                    PaymentMethodId = CardPaymentMethodId,
                    Amount = scenario.Amount,
                    Currency = "USD",
                    IdempotencyKey = idempotencyKey
                });
        });
    }

    private async Task<Exception?> CompleteWithNewContextAsync(
        PaymentScenario scenario,
        Guid paymentId,
        string idempotencyKey)
    {
        await using var context = _fixture.CreateContext();
        return await AsyncTestCapture.CaptureAsync(async () =>
        {
            await CreatePaymentService(context).CompletePaymentAsync(
                paymentId,
                scenario.UserId,
                new CompletePaymentRequestDto
                {
                    IdempotencyKey = idempotencyKey
                });
        });
    }

    private async Task<Exception?> CancelWithNewContextAsync(
        PaymentScenario scenario)
    {
        await using var context = _fixture.CreateContext();
        return await AsyncTestCapture.CaptureAsync(
            () => OrderServiceFactory.Create(context)
                .CancelOrderAsync(scenario.UserId, scenario.OrderId));
    }

    private static PaymentService CreatePaymentService(
        ECommerceDbContext context)
    {
        return new PaymentService(
            context,
            new PaymentRepository(context),
            new OrderRepository(context),
            NullLogger<PaymentService>.Instance);
    }

    private async Task AssertTasksBlockedByOrderLockAsync(
        params Task<Exception?>[] tasks)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        long waiterCount = 0;

        while (DateTime.UtcNow < deadline &&
               tasks.All(task => !task.IsCompleted))
        {
            await using var monitorContext = _fixture.CreateContext();
            waiterCount = await monitorContext.Database
                .SqlQueryRaw<long>("""
                    SELECT COUNT(*)::bigint AS "Value"
                    FROM pg_stat_activity
                    WHERE datname = current_database()
                      AND wait_event_type = 'Lock'
                      AND query LIKE '%FOR UPDATE%'
                    """)
                .SingleAsync();

            if (waiterCount >= tasks.Length)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }

        Assert.True(
            waiterCount >= tasks.Length,
            $"Expected {tasks.Length} PostgreSQL Order-lock waiters but observed {waiterCount}. " +
            string.Join(
                Environment.NewLine,
                tasks.Select(task => task.IsCompleted ? "<completed>" : "<waiting>")));
    }

    private sealed record PaymentScenario(
        OrderScenario Order,
        Guid OrderId,
        decimal Amount)
    {
        public Guid UserId => Order.UserId;
    }
}
