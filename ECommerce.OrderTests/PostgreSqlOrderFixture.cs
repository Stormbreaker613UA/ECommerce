using ECommerce.DAL.DbContexts;
using ECommerce.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Xunit;

namespace ECommerce.OrderTests;

public sealed class PostgreSqlOrderFixture : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public PostgreSqlOrderFixture()
    {
        _connectionString = Environment.GetEnvironmentVariable(
            "ECOMMERCE_TEST_CONNECTION")
            ?? throw new InvalidOperationException(
                "Set ECOMMERCE_TEST_CONNECTION to a dedicated PostgreSQL database before running Order tests.");

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(
            _connectionString);

        var databaseName = connectionStringBuilder.Database;

        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.Contains("OrderTests", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The PostgreSQL test database name must contain 'OrderTests' because the fixture resets it.");
        }

        _databaseName = databaseName;
    }

    public async ValueTask InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public ECommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(_connectionString)
            // The repository has pre-existing Invoice/payment model drift that
            // is intentionally outside this Order repair. The migration under
            // test is still applied; only this design-time warning is ignored.
            .ConfigureWarnings(warnings => warnings.Ignore(
                RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new ECommerceDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        await EnsureDatabaseExistsAsync();
        await ResetAsync();
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        var target = new NpgsqlConnectionStringBuilder(_connectionString);
        target.Database = "postgres";

        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();

        await using var existsCommand = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @database_name",
            connection);
        existsCommand.Parameters.AddWithValue("database_name", _databaseName);

        if (await existsCommand.ExecuteScalarAsync() != null)
            return;

        var quotedDatabaseName = new NpgsqlCommandBuilder()
            .QuoteIdentifier(_databaseName);

        await using var createCommand = new NpgsqlCommand(
            $"CREATE DATABASE {quotedDatabaseName}",
            connection);
        await createCommand.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgreSqlOrderCollection : ICollectionFixture<PostgreSqlOrderFixture>
{
    public const string Name = "PostgreSQL Order repair";
}

public sealed record OrderScenario(
    Guid UserId,
    Guid OtherUserId,
    Guid AddressId,
    Guid OtherAddressId,
    Guid BucketId,
    Guid ProductId,
    Guid SecondProductId,
    Guid CartItemId,
    int InitialStock,
    int Quantity);

public static class OrderTestData
{
    public static async Task<OrderScenario> SeedAsync(
        ECommerceDbContext context,
        int initialStock = 10,
        int quantity = 2,
        bool includeSecondCartItem = false)
    {
        var customerRole = await context.UserRoles
            .SingleAsync(role => role.Name == "Customer");

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var otherAddressId = Guid.NewGuid();
        var bucketId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = $"order-{userId:N}@example.test",
            PasswordHash = "test-password-hash",
            FirstName = "Order",
            LastName = "Customer",
            UserRoleId = customerRole.Id
        };

        var otherUser = new User
        {
            Id = otherUserId,
            Email = $"order-other-{otherUserId:N}@example.test",
            PasswordHash = "test-password-hash",
            FirstName = "Other",
            LastName = "Customer",
            UserRoleId = customerRole.Id
        };

        var category = new Category
        {
            Id = categoryId,
            Name = $"Order test category {categoryId:N}"
        };

        var product = new Product
        {
            Id = productId,
            Name = $"Order test product {productId:N}",
            Description = "Order integration test product",
            Price = 12.50m,
            StockQuantity = initialStock,
            CategoryId = categoryId
        };

        var secondProduct = new Product
        {
            Id = secondProductId,
            Name = $"Order test second product {secondProductId:N}",
            Description = "Second order integration test product",
            Price = 7.25m,
            StockQuantity = initialStock,
            CategoryId = categoryId
        };

        var bucket = new ProductBucket
        {
            Id = bucketId,
            UserId = userId
        };

        var cartItem = new ProductBucketItem
        {
            Id = cartItemId,
            ProductBucketId = bucketId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = product.Price
        };

        context.AddRange(
            user,
            otherUser,
            category,
            product,
            secondProduct,
            bucket,
            cartItem,
            new Address
            {
                Id = addressId,
                UserId = userId,
                Street = "1 Order Street",
                City = "Test City",
                PostalCode = "00001",
                Country = "Test Country",
                IsDefault = true
            },
            new Address
            {
                Id = otherAddressId,
                UserId = otherUserId,
                Street = "2 Other Street",
                City = "Test City",
                PostalCode = "00002",
                Country = "Test Country",
                IsDefault = true
            });

        if (includeSecondCartItem)
        {
            context.ProductBucketItems.Add(new ProductBucketItem
            {
                Id = Guid.NewGuid(),
                ProductBucketId = bucketId,
                ProductId = secondProductId,
                Quantity = quantity,
                UnitPrice = secondProduct.Price
            });
        }

        await context.SaveChangesAsync();

        return new OrderScenario(
            userId,
            otherUserId,
            addressId,
            otherAddressId,
            bucketId,
            productId,
            secondProductId,
            cartItemId,
            initialStock,
            quantity);
    }
}
