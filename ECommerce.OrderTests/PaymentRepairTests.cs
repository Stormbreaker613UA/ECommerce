using ECommerce.BLL.Services.Implementations;
using ECommerce.BLL.Services.Interfaces;
using ECommerce.API.Controllers;
using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.Invoice;
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
            .ToDictionaryAsync(status => status.Status, cancellationToken: TestContext.Current.CancellationToken);
        var methods = await context.PaymentMethods
            .AsNoTracking()
            .ToDictionaryAsync(method => method.Method, cancellationToken: TestContext.Current.CancellationToken);

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
            await blocker.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        Assert.True(await new OrderRepository(blocker).LockOrderRowAsync(scenario.OrderId, TestContext.Current.CancellationToken));

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
            .SingleAsync(item => item.Id == scenario.OrderId, cancellationToken: TestContext.Current.CancellationToken);
        var persistedPayment = await verifyContext.Payments
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == payment.Id, cancellationToken: TestContext.Current.CancellationToken);

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
            await blocker.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        Assert.True(await new OrderRepository(blocker).LockOrderRowAsync(scenario.OrderId, TestContext.Current.CancellationToken));

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
            .SingleAsync(item => item.Id == scenario.OrderId, cancellationToken: TestContext.Current.CancellationToken);
        var payments = await verifyContext.Payments
            .IgnoreQueryFilters()
            .Where(item => item.OrderId == scenario.OrderId)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

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

        var first = await service.CompletePaymentAsync(payment.Id, scenario.UserId, request, TestContext.Current.CancellationToken);
        var second = await service.CompletePaymentAsync(payment.Id, scenario.UserId, request, TestContext.Current.CancellationToken);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.TransactionId, second.TransactionId);
        Assert.Equal(PaymentStatusCatalog.Completed, second.Status);

        await using var verifyContext = _fixture.CreateContext();
        Assert.Equal(
            1,
            await verifyContext.Payments.CountAsync(item => item.Id == payment.Id, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(
            1,
            await verifyContext.Payments.CountAsync(item => item.CompletionIdempotencyKey == "completion-replay", cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(
            1,
            await verifyContext.Invoices.CountAsync(item => item.OrderId == scenario.OrderId, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Competing_completion_keys_can_complete_a_payment_only_once()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "completion-compete-create");

        await using var blocker = _fixture.CreateContext();
        await using var blockerTransaction =
            await blocker.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        Assert.True(await new OrderRepository(blocker).LockOrderRowAsync(scenario.OrderId, TestContext.Current.CancellationToken));

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
            .SingleAsync(item => item.Id == payment.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatusCatalog.CompletedId, persistedPayment.PaymentStatusId);
        Assert.Contains(
            persistedPayment.CompletionIdempotencyKey,
            new[] { "completion-compete-a", "completion-compete-b" });
        Assert.Equal(1, await verifyContext.Payments.CountAsync(item => item.CompletionIdempotencyKey != null, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, await verifyContext.Invoices.CountAsync(item => item.OrderId == scenario.OrderId, cancellationToken: TestContext.Current.CancellationToken));
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
            await blocker.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var orderRepository = new OrderRepository(blocker);

        Assert.True(await orderRepository.LockOrderRowAsync(firstScenario.OrderId, TestContext.Current.CancellationToken));
        Assert.True(await orderRepository.LockOrderRowAsync(secondScenario.OrderId, TestContext.Current.CancellationToken));

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
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        var owner = Assert.Single(owners);
        Assert.Equal(PaymentStatusCatalog.CompletedId, owner.PaymentStatusId);
        Assert.Contains(owner.Id, new[] { firstPayment.Id, secondPayment.Id });
    }

    [Fact]
    public async Task Creation_idempotency_replays_the_same_request_and_rejects_conflicting_data()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        const string idempotencyKey = "creation-replay";

        var first = await CreatePaymentAsync(scenario, idempotencyKey);
        var replay = await CreatePaymentAsync(scenario, idempotencyKey);

        Assert.Equal(first.Id, replay.Id);

        await using var context = _fixture.CreateContext();
        var service = CreatePaymentService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddPaymentAsync(scenario.UserId, new CreatePaymentRequestDto
            {
                OrderId = scenario.OrderId,
                PaymentMethodId = CardPaymentMethodId,
                Amount = scenario.Amount + 1,
                IdempotencyKey = idempotencyKey
            }, TestContext.Current.CancellationToken));

        await using var verifyContext = _fixture.CreateContext();
        Assert.Equal(1, await verifyContext.Payments.CountAsync(payment => payment.OrderId == scenario.OrderId, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Customer_cannot_read_or_mutate_another_users_payment()
    {
        await _fixture.ResetAsync();
        var ownerScenario = await SeedCheckedOutOrderAsync();
        var otherScenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(ownerScenario, "ownership-create");

        await using var context = _fixture.CreateContext();
        var service = CreatePaymentService(context);

        Assert.Null(await service.GetPaymentByIdAsync(payment.Id, otherScenario.UserId, isAdministrator: false, cancellationToken: TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdatePaymentAsync(payment.Id, otherScenario.UserId, isAdministrator: false, new UpdatePaymentRequestDto { PaymentMethodId = CardPaymentMethodId }, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeletePaymentAsync(payment.Id, otherScenario.UserId, isAdministrator: false, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Payment_creation_rejects_invalid_order_amount_and_method()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();

        await using var context = _fixture.CreateContext();
        var service = CreatePaymentService(context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddPaymentAsync(scenario.UserId, new CreatePaymentRequestDto
            {
                OrderId = scenario.OrderId,
                PaymentMethodId = CardPaymentMethodId,
                Amount = scenario.Amount + 1,
                IdempotencyKey = "invalid-amount"
            }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddPaymentAsync(scenario.UserId, new CreatePaymentRequestDto
            {
                OrderId = scenario.OrderId,
                PaymentMethodId = Guid.NewGuid(),
                Amount = scenario.Amount,
                IdempotencyKey = "invalid-method"
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Failed_attempt_is_terminal_and_retriable_while_completed_attempt_is_immutable()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var failedAttempt = await CreatePaymentAsync(scenario, "failed-attempt-create");

        await using (var failContext = _fixture.CreateContext())
        {
            await CreatePaymentService(failContext).DeletePaymentAsync(failedAttempt.Id, scenario.UserId, isAdministrator: false, cancellationToken: TestContext.Current.CancellationToken);
        }

        await using (var failedContext = _fixture.CreateContext())
        {
            var service = CreatePaymentService(failedContext);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompletePaymentAsync(failedAttempt.Id, scenario.UserId, new CompletePaymentRequestDto { IdempotencyKey = "failed-attempt-complete" }, TestContext.Current.CancellationToken));
        }

        var retry = await CreatePaymentAsync(scenario, "retry-after-failure");

        await using (var completionContext = _fixture.CreateContext())
        {
            await CreatePaymentService(completionContext).CompletePaymentAsync(retry.Id, scenario.UserId, new CompletePaymentRequestDto { IdempotencyKey = "completed-attempt" }, TestContext.Current.CancellationToken);
        }

        await using (var immutableContext = _fixture.CreateContext())
        {
            var service = CreatePaymentService(immutableContext);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdatePaymentAsync(retry.Id, scenario.UserId, isAdministrator: false, new UpdatePaymentRequestDto { PaymentMethodId = CardPaymentMethodId }, cancellationToken: TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeletePaymentAsync(retry.Id, scenario.UserId, isAdministrator: false, cancellationToken: TestContext.Current.CancellationToken));
        }

        await using var verifyContext = _fixture.CreateContext();
        var attempts = await verifyContext.Payments
            .Where(payment => payment.OrderId == scenario.OrderId)
            .OrderBy(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, attempts.Count);
        Assert.Contains(attempts, payment =>
            payment.Id == failedAttempt.Id &&
            payment.PaymentStatusId == PaymentStatusCatalog.FailedId);
        Assert.Contains(attempts, payment =>
            payment.Id == retry.Id &&
            payment.PaymentStatusId == PaymentStatusCatalog.CompletedId);
    }

    [Fact]
    public async Task Completion_issues_immutable_invoice_from_the_selected_checkout_address()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "invoice-snapshot-create");

        await using (var completionContext = _fixture.CreateContext())
        {
            await CreatePaymentService(completionContext).CompletePaymentAsync(
                payment.Id,
                scenario.UserId,
                new CompletePaymentRequestDto { IdempotencyKey = "invoice-snapshot-complete" },
                TestContext.Current.CancellationToken);
        }

        InvoiceResponseDto issued;
        await using (var readContext = _fixture.CreateContext())
        {
            var order = await readContext.Orders.SingleAsync(
                item => item.Id == scenario.OrderId,
                TestContext.Current.CancellationToken);
            Assert.Equal(scenario.Order.AddressId, order.BillingAddressId);
            Assert.Equal("1 Order Street, Test City, 00001, Test Country", order.BillingAddressSnapshot);

            issued = (await new InvoiceService(
                readContext,
                new InvoiceRepository(readContext)).GetInvoiceByOrderIdAsync(
                    scenario.OrderId,
                    scenario.UserId,
                    isAdministrator: false,
                    TestContext.Current.CancellationToken))!;

            Assert.StartsWith("INV-", issued.InvoiceNumber);
            Assert.Equal(payment.Id, issued.PaymentId);
            Assert.Equal("USD", issued.Currency);
            Assert.Equal(scenario.Amount, issued.SubtotalAmount);
            Assert.Equal(0m, issued.TaxRate);
            Assert.Equal(0m, issued.TaxAmount);
            Assert.Equal(scenario.Amount, issued.TotalAmount);
            var line = Assert.Single(issued.Items);
            Assert.Equal(scenario.Amount, line.LineTotal);
            Assert.Equal("1 Order Street, Test City, 00001, Test Country", issued.BillingAddress);
        }

        await using (var mutateSourceContext = _fixture.CreateContext())
        {
            var address = await mutateSourceContext.Addresses.IgnoreQueryFilters()
                .SingleAsync(item => item.Id == scenario.Order.AddressId, TestContext.Current.CancellationToken);
            address.Street = "Changed Street";
            address.IsDeleted = true;

            var product = await mutateSourceContext.Products
                .SingleAsync(item => item.Id == scenario.Order.ProductId, TestContext.Current.CancellationToken);
            product.Name = "Changed product name";

            var user = await mutateSourceContext.Users
                .SingleAsync(item => item.Id == scenario.UserId, TestContext.Current.CancellationToken);
            user.Email = "changed@example.test";
            await mutateSourceContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var verifyContext = _fixture.CreateContext();
        var stable = await new InvoiceService(
            verifyContext,
            new InvoiceRepository(verifyContext)).GetInvoiceByIdAsync(
                issued.Id,
                scenario.UserId,
                isAdministrator: false,
                TestContext.Current.CancellationToken);

        Assert.NotNull(stable);
        Assert.Equal(issued.BillingAddress, stable!.BillingAddress);
        Assert.Equal(issued.BillingEmail, stable.BillingEmail);
        Assert.Equal(issued.Items.Single().ProductName, stable.Items.Single().ProductName);
    }

    [Fact]
    public async Task Invoice_generation_failure_rolls_back_completion_to_pending_without_an_invoice()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "invoice-failure-create");

        await using (var failingContext = _fixture.CreateContext())
        {
            var service = CreatePaymentService(failingContext, new FailingInvoiceService());
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompletePaymentAsync(
                payment.Id,
                scenario.UserId,
                new CompletePaymentRequestDto { IdempotencyKey = "invoice-failure-complete" },
                TestContext.Current.CancellationToken));
        }

        await using var verifyContext = _fixture.CreateContext();
        var persistedPayment = await verifyContext.Payments.SingleAsync(
            item => item.Id == payment.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(PaymentStatusCatalog.PendingId, persistedPayment.PaymentStatusId);
        Assert.Null(persistedPayment.CompletionIdempotencyKey);
        Assert.False(await verifyContext.Invoices.AnyAsync(
            item => item.OrderId == scenario.OrderId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Invoice_reads_are_owner_scoped_and_the_controller_exposes_no_mutations()
    {
        await _fixture.ResetAsync();
        var owner = await SeedCheckedOutOrderAsync();
        var other = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(owner, "invoice-owner-create");

        await using (var completionContext = _fixture.CreateContext())
        {
            await CreatePaymentService(completionContext).CompletePaymentAsync(
                payment.Id,
                owner.UserId,
                new CompletePaymentRequestDto { IdempotencyKey = "invoice-owner-complete" },
                TestContext.Current.CancellationToken);
        }

        await using var readContext = _fixture.CreateContext();
        var invoiceService = new InvoiceService(readContext, new InvoiceRepository(readContext));
        var ownerInvoice = await invoiceService.GetInvoiceByOrderIdAsync(
            owner.OrderId, owner.UserId, isAdministrator: false, TestContext.Current.CancellationToken);
        Assert.NotNull(ownerInvoice);
        Assert.Null(await invoiceService.GetInvoiceByIdAsync(
            ownerInvoice!.Id, other.UserId, isAdministrator: false, TestContext.Current.CancellationToken));
        Assert.Null(await invoiceService.GetInvoiceByOrderIdAsync(
            owner.OrderId, other.UserId, isAdministrator: false, TestContext.Current.CancellationToken));
        Assert.NotNull(await invoiceService.GetInvoiceByIdAsync(
            ownerInvoice.Id, other.UserId, isAdministrator: true, TestContext.Current.CancellationToken));

        var mutationActions = typeof(InvoiceController)
            .GetMethods()
            .SelectMany(method => method.GetCustomAttributes(inherit: true))
            .Where(attribute => attribute is Microsoft.AspNetCore.Mvc.HttpPostAttribute ||
                                attribute is Microsoft.AspNetCore.Mvc.HttpPutAttribute ||
                                attribute is Microsoft.AspNetCore.Mvc.HttpDeleteAttribute);
        Assert.Empty(mutationActions);
    }

    [Fact]
    public async Task PostgreSql_enforces_unique_invoice_number_and_order_constraints()
    {
        await _fixture.ResetAsync();
        var owner = await SeedCheckedOutOrderAsync();
        var anotherOrder = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(owner, "invoice-constraint-create");

        await using (var completionContext = _fixture.CreateContext())
        {
            await CreatePaymentService(completionContext).CompletePaymentAsync(
                payment.Id,
                owner.UserId,
                new CompletePaymentRequestDto { IdempotencyKey = "invoice-constraint-complete" },
                TestContext.Current.CancellationToken);
        }

        await using var context = _fixture.CreateContext();
        var issued = await context.Invoices.AsNoTracking().SingleAsync(
            item => item.OrderId == owner.OrderId,
            TestContext.Current.CancellationToken);

        context.Invoices.Add(CreateInvoiceClone(
            issued,
            anotherOrder.OrderId,
            issued.InvoiceNumber));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(
            TestContext.Current.CancellationToken));
        context.ChangeTracker.Clear();

        context.Invoices.Add(CreateInvoiceClone(
            issued,
            owner.OrderId,
            $"INV-{Guid.NewGuid():N}"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Currency_is_server_owned_and_snapshotted_from_checkout_through_invoice()
    {
        await _fixture.ResetAsync();
        var orderScenario = await CreateCheckedOutOrderWithCurrencyAsync("usd");

        Assert.Null(typeof(CreatePaymentRequestDto).GetProperty("Currency"));

        await using (var orderContext = _fixture.CreateContext())
        {
            var order = await orderContext.Orders.SingleAsync(
                item => item.Id == orderScenario.OrderId,
                TestContext.Current.CancellationToken);
            Assert.Equal("USD", order.Currency);

            // A later store configuration affects only future checkout. This
            // existing order remains an immutable USD snapshot.
            _ = OrderServiceFactory.Create(orderContext, storeCurrency: "EUR");
            Assert.Equal("USD", order.Currency);
        }

        var payment = await CreatePaymentAsync(orderScenario, "currency-snapshot-create");
        Assert.Equal("USD", payment.Currency);

        await using (var completionContext = _fixture.CreateContext())
        {
            await CreatePaymentService(completionContext).CompletePaymentAsync(
                payment.Id,
                orderScenario.UserId,
                new CompletePaymentRequestDto { IdempotencyKey = "currency-snapshot-complete" },
                TestContext.Current.CancellationToken);
        }

        await using var verifyContext = _fixture.CreateContext();
        var invoice = await verifyContext.Invoices.SingleAsync(
            item => item.OrderId == orderScenario.OrderId,
            TestContext.Current.CancellationToken);
        Assert.Equal("USD", invoice.Currency);
    }

    [Fact]
    public async Task Incomplete_legacy_invoice_is_not_replayed_and_rolls_back_new_payment_completion()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "legacy-invoice-create");

        await using (var legacyContext = _fixture.CreateContext())
        {
            legacyContext.Invoices.Add(CreateIncompleteLegacyInvoice(scenario.OrderId));
            await legacyContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var completionContext = _fixture.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreatePaymentService(completionContext).CompletePaymentAsync(
                    payment.Id,
                    scenario.UserId,
                    new CompletePaymentRequestDto { IdempotencyKey = "legacy-invoice-complete" },
                    TestContext.Current.CancellationToken));
        }

        await using var verifyContext = _fixture.CreateContext();
        var persistedPayment = await verifyContext.Payments.SingleAsync(
            item => item.Id == payment.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(PaymentStatusCatalog.PendingId, persistedPayment.PaymentStatusId);
        Assert.Null(persistedPayment.CompletionIdempotencyKey);
        Assert.Single(await verifyContext.Invoices.Where(
            item => item.OrderId == scenario.OrderId).ToListAsync(
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PostgreSql_rejects_a_nonexistent_non_null_invoice_payment_id()
    {
        await _fixture.ResetAsync();
        var owner = await SeedCheckedOutOrderAsync();
        var anotherOrder = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(owner, "invoice-payment-fk-create");

        await using (var completionContext = _fixture.CreateContext())
        {
            await CreatePaymentService(completionContext).CompletePaymentAsync(
                payment.Id,
                owner.UserId,
                new CompletePaymentRequestDto { IdempotencyKey = "invoice-payment-fk-complete" },
                TestContext.Current.CancellationToken);
        }

        await using var context = _fixture.CreateContext();
        var issued = await context.Invoices.AsNoTracking().SingleAsync(
            item => item.OrderId == owner.OrderId,
            TestContext.Current.CancellationToken);
        var orphan = CreateInvoiceClone(
            issued,
            anotherOrder.OrderId,
            $"INV-{Guid.NewGuid():N}");
        orphan.PaymentId = Guid.NewGuid();
        context.Invoices.Add(orphan);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Financially_inconsistent_existing_invoice_is_rejected_and_completion_rolls_back()
    {
        await _fixture.ResetAsync();
        var scenario = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(scenario, "invalid-totals-create");
        var invoiceId = Guid.NewGuid();
        var invoiceItemId = Guid.NewGuid();

        // The trigger observes the server-generated transaction ID from the
        // guarded completion update. It creates a correctly linked Invoice
        // with deliberately inconsistent totals in that same transaction.
        await using (var triggerContext = _fixture.CreateContext())
        {
#pragma warning disable EF1002 // The interpolated values are test-generated Guid literals in PostgreSQL DDL.
            await triggerContext.Database.ExecuteSqlRawAsync($"""
                CREATE OR REPLACE FUNCTION "CreateInvalidInvoiceOnCompletion"()
                RETURNS trigger AS $function$
                BEGIN
                    IF NEW."Id" = '{payment.Id}' THEN
                        INSERT INTO "Invoices" (
                            "Id", "OrderId", "PaymentId", "PaymentTransactionId",
                            "InvoiceNumber", "Currency", "BillingEmail",
                            "BillingFirstName", "BillingLastName", "BillingAddress",
                            "SubtotalAmount", "TotalAmount", "TaxAmount", "TaxRate",
                            "IssuedAt", "CreatedAt", "IsDeleted")
                        VALUES (
                            '{invoiceId}', NEW."OrderId", NEW."Id", NEW."TransactionId",
                            'INV-invalid-totals', NEW."Currency", 'invoice@example.test',
                            'Invoice', 'Test', 'Snapshot address',
                            2.00, 2.00, 0.00, 0.00, NOW(), NOW(), FALSE);

                        INSERT INTO "InvoiceItems" (
                            "Id", "InvoiceId", "ProductName", "Quantity", "UnitPrice", "LineTotal")
                        VALUES ('{invoiceItemId}', '{invoiceId}', 'Snapshot product', 1, 1.00, 1.00);
                    END IF;
                    RETURN NEW;
                END;
                $function$ LANGUAGE plpgsql;

                CREATE TRIGGER "CreateInvalidInvoiceOnCompletionTrigger"
                AFTER UPDATE OF "PaymentStatusId" ON "Payments"
                FOR EACH ROW
                WHEN (NEW."PaymentStatusId" = '{PaymentStatusCatalog.CompletedId}')
                EXECUTE FUNCTION "CreateInvalidInvoiceOnCompletion"();
                """, TestContext.Current.CancellationToken);
#pragma warning restore EF1002
        }

        await using (var completionContext = _fixture.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                CreatePaymentService(completionContext).CompletePaymentAsync(
                    payment.Id,
                    scenario.UserId,
                    new CompletePaymentRequestDto { IdempotencyKey = "invalid-totals-complete" },
                    TestContext.Current.CancellationToken));
        }

        await using var verifyContext = _fixture.CreateContext();
        var persistedPayment = await verifyContext.Payments.SingleAsync(
            item => item.Id == payment.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(PaymentStatusCatalog.PendingId, persistedPayment.PaymentStatusId);
        Assert.False(await verifyContext.Invoices.AnyAsync(
            item => item.OrderId == scenario.OrderId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Invoice_reads_and_replay_survive_order_soft_deletion_without_cross_user_access()
    {
        await _fixture.ResetAsync();
        var owner = await SeedCheckedOutOrderAsync();
        var other = await SeedCheckedOutOrderAsync();
        var payment = await CreatePaymentAsync(owner, "soft-deleted-order-create");

        await using (var completionContext = _fixture.CreateContext())
        {
            await CreatePaymentService(completionContext).CompletePaymentAsync(
                payment.Id,
                owner.UserId,
                new CompletePaymentRequestDto { IdempotencyKey = "soft-deleted-order-complete" },
                TestContext.Current.CancellationToken);
        }

        Guid invoiceId;
        await using (var deleteContext = _fixture.CreateContext())
        {
            invoiceId = await deleteContext.Invoices
                .Where(invoice => invoice.OrderId == owner.OrderId)
                .Select(invoice => invoice.Id)
                .SingleAsync(TestContext.Current.CancellationToken);

            var order = await deleteContext.Orders.IgnoreQueryFilters().SingleAsync(
                item => item.Id == owner.OrderId,
                TestContext.Current.CancellationToken);
            order.IsDeleted = true;
            await deleteContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = _fixture.CreateContext();
        var invoiceService = new InvoiceService(readContext, new InvoiceRepository(readContext));
        Assert.NotNull(await invoiceService.GetInvoiceByIdAsync(
            invoiceId, owner.UserId, isAdministrator: false, TestContext.Current.CancellationToken));
        Assert.NotNull(await invoiceService.GetInvoiceByIdAsync(
            invoiceId, other.UserId, isAdministrator: true, TestContext.Current.CancellationToken));
        Assert.Null(await invoiceService.GetInvoiceByIdAsync(
            invoiceId, other.UserId, isAdministrator: false, TestContext.Current.CancellationToken));

        var replay = await invoiceService.EnsureInvoiceForCompletedPaymentAsync(
            payment.Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(invoiceId, replay.Id);
        Assert.Equal(1, await readContext.Invoices.IgnoreQueryFilters().CountAsync(
            invoice => invoice.OrderId == owner.OrderId,
            TestContext.Current.CancellationToken));
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

    private async Task<PaymentScenario> CreateCheckedOutOrderWithCurrencyAsync(
        string storeCurrency)
    {
        await using var context = _fixture.CreateContext();
        var orderScenario = await OrderTestData.SeedAsync(context);
        var order = await OrderServiceFactory.Create(
                context,
                storeCurrency: storeCurrency)
            .CheckoutAsync(
                orderScenario.UserId,
                new ECommerce.DAL.DTOs.Order.CheckoutDto
                {
                    AddressId = orderScenario.AddressId
                },
                TestContext.Current.CancellationToken);

        return new PaymentScenario(orderScenario, order.Id, order.TotalAmount);
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
        ECommerceDbContext context,
        IInvoiceService? invoiceService = null)
    {
        return new PaymentService(
            context,
            new PaymentRepository(context),
            new OrderRepository(context),
            invoiceService ?? new InvoiceService(context, new InvoiceRepository(context)),
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

    private static Invoice CreateInvoiceClone(
        Invoice issued,
        Guid orderId,
        string invoiceNumber) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        InvoiceNumber = invoiceNumber,
        Currency = issued.Currency,
        PaymentTransactionId = issued.PaymentTransactionId,
        BillingEmail = issued.BillingEmail,
        BillingFirstName = issued.BillingFirstName,
        BillingLastName = issued.BillingLastName,
        BillingAddress = issued.BillingAddress,
        SubtotalAmount = issued.SubtotalAmount,
        TaxRate = issued.TaxRate,
        TaxAmount = issued.TaxAmount,
        TotalAmount = issued.TotalAmount,
        IssuedAt = issued.IssuedAt
    };

    private static Invoice CreateIncompleteLegacyInvoice(Guid orderId) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = orderId,
        InvoiceNumber = $"LEGACY-{Guid.NewGuid():N}",
        Currency = "USD",
        PaymentTransactionId = string.Empty,
        BillingEmail = "legacy@example.test",
        BillingFirstName = "Legacy",
        BillingLastName = "Invoice",
        BillingAddress = "Legacy address",
        IssuedAt = DateTime.UtcNow
    };

    private sealed record PaymentScenario(
        OrderScenario Order,
        Guid OrderId,
        decimal Amount)
    {
        public Guid UserId => Order.UserId;
    }

    private sealed class FailingInvoiceService : IInvoiceService
    {
        public Task<InvoiceResponseDto> EnsureInvoiceForCompletedPaymentAsync(
            Guid paymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromException<InvoiceResponseDto>(
                new InvalidOperationException("Forced invoice generation failure."));

        public Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId, Guid userId, bool isAdministrator, CancellationToken cancellationToken = default) =>
            Task.FromResult<InvoiceResponseDto?>(null);

        public Task<InvoiceResponseDto?> GetInvoiceByOrderIdAsync(Guid orderId, Guid userId, bool isAdministrator, CancellationToken cancellationToken = default) =>
            Task.FromResult<InvoiceResponseDto?>(null);

        public Task<InvoicePageResponseDto> GetInvoicesAsync(Guid? userId, bool isAdministrator, InvoicePageRequestDto request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new InvoicePageResponseDto());
    }
}
