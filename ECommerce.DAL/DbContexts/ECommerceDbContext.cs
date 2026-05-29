using ECommerce.DAL.Entities;
using Microsoft.EntityFrameworkCore;


namespace ECommerce.DAL.DbContexts;

public class ECommerceDbContext : DbContext
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)
    {

    }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(ct);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);

            e.HasOne(x => x.UserRole)
             .WithMany(x => x.Users)
             .HasForeignKey(x => x.UserRoleId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ProductBucket)
             .WithOne(x => x.User)
             .HasForeignKey<ProductBucket>(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Addresses)
             .WithOne(x => x.User)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Orders)
             .WithOne(x => x.User)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Reviews)
             .WithOne(x => x.User)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);


        });
        modelBuilder.Entity<UserRole>(ur =>
        {
            ur.HasKey(x => x.Id);
            ur.Property(x => x.Name).IsRequired().HasMaxLength(100);
            ur.Property(x => x.Description).HasMaxLength(500);
            ur.Property(x => x.IsActive).HasDefaultValue(true);
            ur.HasIndex(x => x.Name).IsUnique();

            ur.HasData(
                new UserRole
                {
                    Id = new Guid("a1b2c3d4-0000-0000-0000-000000000001"),
                    Name = "Admin",
                    Description = "Administrator with full access",
                    IsActive = true,
                },
                new UserRole
                {
                    Id = new Guid("a1b2c3d4-0000-0000-0000-000000000002"),
                    Name = "Customer",
                    Description = "Regular customer with limited access",
                    IsActive = true,                  
                }
            );

        });

        modelBuilder.Entity<Address>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.Street).IsRequired().HasMaxLength(200);
            a.Property(x => x.City).IsRequired().HasMaxLength(100);
            a.Property(x => x.State).HasMaxLength(100);
            a.Property(x => x.PostalCode).IsRequired().HasMaxLength(20);
            a.Property(x => x.Country).IsRequired().HasMaxLength(100);
            a.Property(x => x.IsDefault).HasDefaultValue(false);
            a.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Category>(c =>
        {
            c.HasKey(x => x.Id);
            c.Property(x => x.Name).IsRequired().HasMaxLength(100);
            c.Property(x => x.Description).HasMaxLength(500);
            c.HasIndex(x => x.Name).IsUnique();
            c.HasQueryFilter(x => !x.IsDeleted);
            c.HasOne(x => x.ParentCategory)
             .WithMany(x => x.SubCategories)
             .HasForeignKey(x => x.ParentCategoryId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired(false);
        });
        modelBuilder.Entity<Product>(p =>
        {
            p.HasKey(x => x.Id);
            p.Property(x => x.Name).IsRequired().HasMaxLength(200);
            p.Property(x => x.Description).HasMaxLength(2000);
            p.Property(x => x.Price).IsRequired().HasPrecision(18, 2);
            p.Property(x => x.StockQuantity).IsRequired().HasDefaultValue(0);
            p.HasIndex(x => x.Name);
            p.HasQueryFilter(x => !x.IsDeleted);

            p.HasOne(x => x.Category)
             .WithMany(x => x.Products)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            p.HasMany(x => x.ProductImages)
             .WithOne(x => x.Product)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            p.HasMany(x => x.Reviews)
             .WithOne(x => x.Product)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            p.HasMany(x => x.ProductBucketItems)
             .WithOne(x => x.Product)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            p.HasMany(x => x.OrderItems)
             .WithOne(x => x.Product)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProductImage>(pi =>
        {
            pi.HasKey(x => x.Id);
            pi.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
            pi.HasOne(x => x.Product)
              .WithMany(x => x.ProductImages)
              .HasForeignKey(x => x.ProductId)
              .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProductBucket>(pb =>
        {
            pb.HasKey(x => x.Id);
            pb.HasIndex(x => x.UserId).IsUnique();
            pb.HasQueryFilter(x => !x.IsDeleted);
            pb.HasOne(x => x.User)
              .WithOne(x => x.ProductBucket)
              .HasForeignKey<ProductBucket>(x => x.UserId)
              .OnDelete(DeleteBehavior.Cascade);
            pb.HasMany(x => x.ProductBucketItems)
              .WithOne(x => x.ProductBucket)
              .HasForeignKey(x => x.ProductBucketId)
              .OnDelete(DeleteBehavior.Cascade);
        }
        );
        modelBuilder.Entity<ProductBucketItem>(pbi =>
        {
            pbi.HasKey(x => x.Id);
            pbi.Property(x => x.Quantity).IsRequired().HasDefaultValue(1);
            pbi.HasIndex(x => new { x.ProductBucketId, x.ProductId }).IsUnique();
            pbi.HasQueryFilter(x => !x.IsDeleted);
            pbi.HasOne(x => x.Product)
               .WithMany(x => x.ProductBucketItems)
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
            pbi.HasOne(x => x.ProductBucket)
               .WithMany(x => x.ProductBucketItems)
               .HasForeignKey(x => x.ProductBucketId)
               .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Order>(o =>
        {
            o.HasKey(x => x.Id);
            o.Property(x => x.OrderDate).IsRequired();
            o.Property(x => x.TotalAmount).IsRequired().HasPrecision(18, 2);
            o.HasQueryFilter(x => !x.IsDeleted);
            o.HasOne(x => x.User)
             .WithMany(x => x.Orders)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            o.HasMany(x => x.OrderItems)
             .WithOne(x => x.Order)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<OrderItem>(oi =>
        {
            oi.HasKey(x => x.Id);
            oi.Property(x => x.Quantity).IsRequired().HasDefaultValue(1);
            oi.Property(x => x.UnitPrice).IsRequired().HasPrecision(18, 2);
            oi.HasQueryFilter(x => !x.IsDeleted);
            oi.HasOne(x => x.Product)
               .WithMany(x => x.OrderItems)
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
            oi.HasOne(x => x.Order)
               .WithMany(x => x.OrderItems)
               .HasForeignKey(x => x.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<OrderStatus>(os =>
        {
            os.HasKey(x => x.Id);
            os.Property(x => x.Name).IsRequired().HasMaxLength(100);
            os.Property(x => x.Description).HasMaxLength(500);
            os.Property(x => x.IsActive).HasDefaultValue(true);
            os.HasIndex(x => x.Name).IsUnique();
            os.HasData(
                new OrderStatus { Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000001"), Name = "Pending", CanCancel = true, CanRefund = false, CanShip = false, IsActive = true },
                new OrderStatus { Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000002"), Name = "Confirmed", CanCancel = true, CanRefund = false, CanShip = true, IsActive = true },
                new OrderStatus { Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000003"), Name = "Shipped", CanCancel = false, CanRefund = false, CanShip = false, IsActive = true },
                new OrderStatus { Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000004"), Name = "Delivered", CanCancel = false, CanRefund = true, CanShip = false, IsActive = true },
                new OrderStatus { Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000005"), Name = "Cancelled", CanCancel = false, CanRefund = false, CanShip = false, IsActive = true },
                new OrderStatus { Id = Guid.Parse("b1b2c3d4-0000-0000-0000-000000000006"), Name = "Refunded", CanCancel = false, CanRefund = false, CanShip = false, IsActive = true }
            );

            os.HasMany(x => x.Orders)
             .WithOne(x => x.OrderStatus)
             .HasForeignKey(x => x.OrderStatusId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Invoice>(i =>
        {
            i.HasKey(x => x.Id);
            i.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            i.Property(x => x.TotalAmount).IsRequired().HasPrecision(18, 2);
            i.Property(x => x.TaxAmount).IsRequired().HasPrecision(18, 2);
            i.Property(x => x.TaxRate).IsRequired().HasPrecision(5, 2);
            i.Property(x => x.BillingEmail).IsRequired().HasMaxLength(256);
            i.Property(x => x.BillingFirstName).IsRequired().HasMaxLength(100);
            i.Property(x => x.BillingLastName).IsRequired().HasMaxLength(100);
            i.Property(x => x.BillingAddress).IsRequired().HasMaxLength(500);
            i.Property(x => x.Notes).HasMaxLength(500);
            i.HasIndex(x => x.InvoiceNumber).IsUnique();
            i.HasQueryFilter(x => !x.IsDeleted);
        });
        modelBuilder.Entity<Review>(r =>
        {
            r.HasKey(x => x.Id);
            r.Property(x => x.Rating).IsRequired();
            r.Property(x => x.CommentText).HasMaxLength(1000);
            r.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique(); // one review per user per product
            r.HasQueryFilter(x => !x.IsDeleted);

            r.HasOne(x => x.User)
             .WithMany(x => x.Reviews)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            r.HasOne(x => x.Product)
             .WithMany(x => x.Reviews)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Payment>(p =>
        {
            p.HasKey(x => x.Id);
            p.Property(x => x.Amount).IsRequired().HasPrecision(18, 2);
            p.Property(x => x.PaidAt).IsRequired();
            p.HasQueryFilter(x => !x.IsDeleted);

            p.HasOne(x => x.Order)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Restrict);
            p.HasOne(x => x.PaymentStatus)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.PaymentStatusId)
             .OnDelete(DeleteBehavior.Restrict);
            p.HasOne(x => x.PaymentMethod)
             .WithMany(x => x.Payments)
             .HasForeignKey(x => x.PaymentMethodId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PaymentStatus>(ps =>
        {
            ps.HasKey(x => x.Id);
            ps.Property(x => x.Status).IsRequired().HasMaxLength(50);
            ps.HasIndex(x => x.Status).IsUnique();
        });
        modelBuilder.Entity<PaymentMethod>(pm =>
        {
            pm.HasKey(x => x.Id);
            pm.Property(x => x.Method).IsRequired().HasMaxLength(50);
            pm.HasIndex(x => x.Method).IsUnique();
        });
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductBucket> ProductBuckets => Set<ProductBucket>();
    public DbSet<ProductBucketItem> ProductBucketItems => Set<ProductBucketItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentStatus> PaymentStatuses => Set<PaymentStatus>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

}
