using Microsoft.EntityFrameworkCore;
using WonderlandBackend.Models;

namespace WonderlandBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============ USER CONFIGURATIONS ============
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.PasswordHash).IsRequired();
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.Role).HasMaxLength(20).HasDefaultValue("Customer");
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // ============ REFRESH TOKEN CONFIGURATIONS ============
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.JwtId).IsRequired();
                entity.Property(rt => rt.ExpiryDate).IsRequired();
                entity.Property(rt => rt.CreatedAt).HasDefaultValueSql("NOW()");
                entity.HasIndex(rt => rt.Token).IsUnique();
                entity.HasIndex(rt => rt.JwtId);
                entity.HasIndex(rt => new { rt.UserId, rt.IsRevoked });

                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ PRODUCT CONFIGURATIONS ============
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.Category).HasMaxLength(50);
                entity.Property(p => p.ImageUrl).HasMaxLength(500);
                entity.Property(p => p.StockQuantity).HasDefaultValue(0);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");
                entity.HasIndex(p => p.Name);
                entity.HasIndex(p => p.Category);
            });

            // ============ CART CONFIGURATIONS ============
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");

                entity.HasOne(c => c.User)
                    .WithOne(u => u.Cart)
                    .HasForeignKey<Cart>(c => c.UserId);

                entity.HasMany(c => c.CartItems)
                    .WithOne(ci => ci.Cart)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ CARTITEM CONFIGURATIONS ============
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.Property(ci => ci.Quantity).HasDefaultValue(1);
                entity.HasIndex(ci => new { ci.CartId, ci.ProductId }).IsUnique();
                entity.HasOne(ci => ci.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(ci => ci.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ============ ORDER CONFIGURATIONS ============
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
                entity.Property(o => o.Status).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(500);
                entity.Property(o => o.OrderDate).HasDefaultValueSql("NOW()");

                entity.HasIndex(o => o.OrderDate);
                entity.HasIndex(o => new { o.UserId, o.OrderDate });

                entity.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(o => o.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ============ ORDERITEM CONFIGURATIONS ============
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
                entity.Property(oi => oi.Quantity).IsRequired();
                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}