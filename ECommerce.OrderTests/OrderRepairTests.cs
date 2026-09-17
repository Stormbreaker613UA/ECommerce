using System.Security.Claims;
using ECommerce.API.Controllers;
using ECommerce.BLL.Services.Implementations;
using ECommerce.DAL.DbContexts;
using ECommerce.DAL.DTOs.Order;
using ECommerce.DAL.Entities;
using ECommerce.DAL.Repositories.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.OrderTests;

[Collection(PostgreSqlOrderCollection.Name)]
public sealed class OrderRepairTests
{
    private readonly PostgreSqlOrderFixture _fixture;

    public OrderRepairTests(PostgreSqlOrderFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Checkout_soft_deletes_history_and_allows_readding_same_product()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;
        GetOrderDto order;

        await using (var context = _fixture.CreateContext())
        {
            scenario = await OrderTestData.SeedAsync(context);
            order = await OrderServiceFactory.Create(context)
                .CheckoutAsync(
                    scenario.UserId,
                    new CheckoutDto { AddressId = scenario.AddressId });
        }

        Assert.Equal("Pending", order.Status);
        Assert.Single(order.Items);

        await using var verifyContext = _fixture.CreateContext();
        var historicalItems = await verifyContext.ProductBucketItems
            .IgnoreQueryFilters()
            .Where(item =>
                item.ProductBucketId == scenario.BucketId &&
                item.ProductId == scenario.ProductId)
            .ToListAsync();

        Assert.Single(historicalItems);
        Assert.True(historicalItems[0].IsDeleted);
        Assert.NotNull(historicalItems[0].DeletedAt);

        var persistedOrder = await verifyContext.Orders
            .Include(item => item.OrderItems)
            .SingleAsync(item => item.Id == order.Id);
        Assert.Single(persistedOrder.OrderItems);
        Assert.Equal(scenario.Quantity, persistedOrder.OrderItems.First().Quantity);

        var productAfterCheckout = await verifyContext.Products
            .SingleAsync(item => item.Id == scenario.ProductId);
        Assert.Equal(
            scenario.InitialStock - scenario.Quantity,
            productAfterCheckout.StockQuantity);
        Assert.NotNull(productAfterCheckout.UpdatedAt);

        var repository = new ProductBucketRepository(verifyContext);
        await repository.AddItemAsync(new ProductBucketItem
        {
            Id = Guid.NewGuid(),
            ProductBucketId = scenario.BucketId,
            ProductId = scenario.ProductId,
            Quantity = 1,
            UnitPrice = 12.50m
        });

        verifyContext.ProductBucketItems.AddRange(
            new ProductBucketItem
            {
                Id = Guid.NewGuid(),
                ProductBucketId = scenario.BucketId,
                ProductId = scenario.ProductId,
                Quantity = 3,
                UnitPrice = 12.50m,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            },
            new ProductBucketItem
            {
                Id = Guid.NewGuid(),
                ProductBucketId = scenario.BucketId,
                ProductId = scenario.ProductId,
                Quantity = 4,
                UnitPrice = 12.50m,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            });
        await verifyContext.SaveChangesAsync();

        var allRows = await verifyContext.ProductBucketItems
            .IgnoreQueryFilters()
            .Where(item =>
                item.ProductBucketId == scenario.BucketId &&
                item.ProductId == scenario.ProductId)
            .ToListAsync();

        Assert.Equal(4, allRows.Count);
        Assert.Single(allRows, item => !item.IsDeleted);
        Assert.Equal(3, allRows.Count(item => item.IsDeleted));

        await using var duplicateContext = _fixture.CreateContext();
        duplicateContext.ProductBucketItems.Add(new ProductBucketItem
        {
            Id = Guid.NewGuid(),
            ProductBucketId = scenario.BucketId,
            ProductId = scenario.ProductId,
            Quantity = 1,
            UnitPrice = 12.50m
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Cart_snapshot_mismatch_rolls_back_order_stock_and_cart_clear()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;

        await using (var seedContext = _fixture.CreateContext())
            scenario = await OrderTestData.SeedAsync(seedContext);

        await using (var checkoutContext = _fixture.CreateContext())
        {
            var innerRepository = new ProductBucketRepository(checkoutContext);
            var mutatingRepository = new AddCartItemBeforeClearRepository(
                innerRepository,
                async () =>
                {
                    await using var mutationContext = _fixture.CreateContext();
                    mutationContext.ProductBucketItems.Add(new ProductBucketItem
                    {
                        Id = Guid.NewGuid(),
                        ProductBucketId = scenario.BucketId,
                        ProductId = scenario.SecondProductId,
                        Quantity = 1,
                        UnitPrice = 7.25m
                    });
                    await mutationContext.SaveChangesAsync();
                });

            var service = OrderServiceFactory.Create(
                checkoutContext,
                mutatingRepository);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CheckoutAsync(
                    scenario.UserId,
                    new CheckoutDto { AddressId = scenario.AddressId }));
        }

        await using var verifyContext = _fixture.CreateContext();
        Assert.Equal(
            0,
            await verifyContext.Orders.CountAsync());

        var product = await verifyContext.Products
            .SingleAsync(item => item.Id == scenario.ProductId);
        Assert.Equal(scenario.InitialStock, product.StockQuantity);

        var activeItems = await verifyContext.ProductBucketItems
            .Where(item => item.ProductBucketId == scenario.BucketId)
            .ToListAsync();
        Assert.Equal(2, activeItems.Count);
        Assert.Contains(activeItems, item => item.ProductId == scenario.ProductId);
        Assert.Contains(activeItems, item => item.ProductId == scenario.SecondProductId);
    }

    [Fact]
    public async Task Simultaneous_checkouts_cannot_make_stock_negative()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;

        await using (var seedContext = _fixture.CreateContext())
        {
            scenario = await OrderTestData.SeedAsync(
                seedContext,
                initialStock: 1,
                quantity: 1);
        }

        var checkoutTasks = new[]
        {
            CheckoutWithNewContextAsync(scenario),
            CheckoutWithNewContextAsync(scenario)
        };

        var errors = await Task.WhenAll(checkoutTasks);

        Assert.Single(errors, error => error == null);
        Assert.Single(errors, error => error != null);

        await using var verifyContext = _fixture.CreateContext();
        var product = await verifyContext.Products
            .SingleAsync(item => item.Id == scenario.ProductId);
        Assert.Equal(0, product.StockQuantity);
        Assert.True(product.StockQuantity >= 0);
        Assert.Equal(1, await verifyContext.Orders.CountAsync());
    }

    [Fact]
    public async Task Concurrent_and_repeated_cancellation_restore_stock_exactly_once()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;
        Guid orderId;

        await using (var checkoutContext = _fixture.CreateContext())
        {
            scenario = await OrderTestData.SeedAsync(checkoutContext);
            var order = await OrderServiceFactory.Create(checkoutContext)
                .CheckoutAsync(
                    scenario.UserId,
                    new CheckoutDto { AddressId = scenario.AddressId });
            orderId = order.Id;
        }

        var cancellationTasks = new[]
        {
            CancelWithNewContextAsync(scenario.UserId, orderId),
            CancelWithNewContextAsync(scenario.UserId, orderId)
        };

        var errors = await Task.WhenAll(cancellationTasks);

        Assert.Single(errors, error => error == null);
        Assert.Single(errors, error => error is InvalidOperationException);

        await using (var repeatedContext = _fixture.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => OrderServiceFactory.Create(repeatedContext)
                    .CancelOrderAsync(scenario.UserId, orderId));
        }

        await using var verifyContext = _fixture.CreateContext();
        var product = await verifyContext.Products
            .SingleAsync(item => item.Id == scenario.ProductId);
        Assert.Equal(scenario.InitialStock, product.StockQuantity);
        Assert.NotNull(product.UpdatedAt);

        var orderEntity = await verifyContext.Orders
            .Include(item => item.OrderStatus)
            .SingleAsync(item => item.Id == orderId);
        Assert.Equal("Cancelled", orderEntity.OrderStatus.Name);
        Assert.NotNull(orderEntity.UpdatedAt);
    }

    [Fact]
    public async Task Cancellation_restores_stock_after_product_soft_deletion()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;
        Guid orderId;

        await using (var checkoutContext = _fixture.CreateContext())
        {
            scenario = await OrderTestData.SeedAsync(checkoutContext);
            orderId = (await OrderServiceFactory.Create(checkoutContext)
                .CheckoutAsync(
                    scenario.UserId,
                    new CheckoutDto { AddressId = scenario.AddressId })).Id;
        }

        await using (var deleteContext = _fixture.CreateContext())
        {
            var product = await deleteContext.Products
                .SingleAsync(item => item.Id == scenario.ProductId);
            deleteContext.Products.Remove(product);
            await deleteContext.SaveChangesAsync();
        }

        await using (var cancelContext = _fixture.CreateContext())
        {
            await OrderServiceFactory.Create(cancelContext)
                .CancelOrderAsync(scenario.UserId, orderId);
        }

        await using var verifyContext = _fixture.CreateContext();
        var productAfterCancellation = await verifyContext.Products
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == scenario.ProductId);
        Assert.True(productAfterCancellation.IsDeleted);
        Assert.Equal(scenario.InitialStock, productAfterCancellation.StockQuantity);
        Assert.NotNull(productAfterCancellation.UpdatedAt);

        var order = await verifyContext.Orders
            .Include(item => item.OrderStatus)
            .SingleAsync(item => item.Id == orderId);
        Assert.Equal("Cancelled", order.OrderStatus.Name);
    }

    [Fact]
    public async Task Missing_physical_product_leaves_status_and_other_stock_unchanged()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;
        Guid orderId;

        await using (var checkoutContext = _fixture.CreateContext())
        {
            scenario = await OrderTestData.SeedAsync(
                checkoutContext,
                includeSecondCartItem: true);
            orderId = (await OrderServiceFactory.Create(checkoutContext)
                .CheckoutAsync(
                    scenario.UserId,
                    new CheckoutDto { AddressId = scenario.AddressId })).Id;
        }

        await using (var deleteContext = _fixture.CreateContext())
        {
            await deleteContext.Database.ExecuteSqlInterpolatedAsync($"""
                ALTER TABLE "OrderItems" DROP CONSTRAINT IF EXISTS "FK_OrderItems_Products_ProductId";
                ALTER TABLE "ProductBucketItems" DROP CONSTRAINT IF EXISTS "FK_ProductBucketItems_Products_ProductId";
                DELETE FROM "Products" WHERE "Id" = {scenario.SecondProductId};
                """);
        }

        await using (var cancelContext = _fixture.CreateContext())
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => OrderServiceFactory.Create(cancelContext)
                    .CancelOrderAsync(scenario.UserId, orderId));
        }

        await using var verifyContext = _fixture.CreateContext();
        var order = await verifyContext.Orders
            .Include(item => item.OrderStatus)
            .SingleAsync(item => item.Id == orderId);
        Assert.Equal("Pending", order.OrderStatus.Name);

        var remainingProduct = await verifyContext.Products
            .SingleAsync(item => item.Id == scenario.ProductId);
        Assert.Equal(
            scenario.InitialStock - scenario.Quantity,
            remainingProduct.StockQuantity);

        Assert.False(await verifyContext.Products
            .IgnoreQueryFilters()
            .AnyAsync(item => item.Id == scenario.SecondProductId));
    }

    [Fact]
    public async Task Cross_user_read_and_cancellation_are_hidden_as_404()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;
        Guid orderId;

        await using (var checkoutContext = _fixture.CreateContext())
        {
            scenario = await OrderTestData.SeedAsync(checkoutContext);
            orderId = (await OrderServiceFactory.Create(checkoutContext)
                .CheckoutAsync(
                    scenario.UserId,
                    new CheckoutDto { AddressId = scenario.AddressId })).Id;
        }

        await using var otherUserContext = _fixture.CreateContext();
        var otherUserService = OrderServiceFactory.Create(otherUserContext);

        Assert.Null(await otherUserService.GetOrderByIdAsync(
            orderId,
            scenario.OtherUserId));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => otherUserService.CancelOrderAsync(
                scenario.OtherUserId,
                orderId));

        var controller = new OrderController(otherUserService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateAuthenticatedContext(scenario.OtherUserId)
            }
        };

        var result = await controller.GetById(orderId, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Checkout_controller_returns_201_with_customer_get_by_id_location_metadata()
    {
        await _fixture.ResetAsync();

        OrderScenario scenario;

        await using var context = _fixture.CreateContext();
        scenario = await OrderTestData.SeedAsync(context);

        var controller = new OrderController(OrderServiceFactory.Create(context))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateAuthenticatedContext(scenario.UserId)
            }
        };

        var result = await controller.Checkout(
            new CheckoutDto { AddressId = scenario.AddressId },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(OrderController.GetById), created.ActionName);
        Assert.NotNull(created.RouteValues);
        Assert.Equal(
            Assert.IsType<Guid>(created.RouteValues!["id"]),
            Assert.IsType<GetOrderDto>(created.Value).Id);
    }

    private async Task<Exception?> CheckoutWithNewContextAsync(
        OrderScenario scenario)
    {
        await using var context = _fixture.CreateContext();
        var service = OrderServiceFactory.Create(context);

        return await AsyncTestCapture.CaptureAsync(
            () => service.CheckoutAsync(
                scenario.UserId,
                new CheckoutDto { AddressId = scenario.AddressId }));
    }

    private async Task<Exception?> CancelWithNewContextAsync(
        Guid userId,
        Guid orderId)
    {
        await using var context = _fixture.CreateContext();
        var service = OrderServiceFactory.Create(context);

        return await AsyncTestCapture.CaptureAsync(
            () => service.CancelOrderAsync(userId, orderId));
    }

    private static DefaultHttpContext CreateAuthenticatedContext(Guid userId)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            },
            "test");

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
    }
}
