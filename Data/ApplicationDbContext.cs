using AlDentev2.Models;
using Microsoft.EntityFrameworkCore;

namespace AlDentev2.Data
{
    /// <summary>
    /// Łączy modele z bazą danych
    /// </summary>
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        //DbSet to kolekcja reprezentująca tabelę w bazie danych
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Size> Sizes { get; set; } = null!;
        public DbSet<ProductSize> ProductSizes { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<ShippingMethod> ShippingMethods { get; set; } = null!;
        public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //konfiguracja relacji i indeksów
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<ProductSize>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSizes)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductSize>()
               .HasOne(ps => ps.Size)
               .WithMany(s => s.ProductSizes)
               .HasForeignKey(ps => ps.SizeId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.DefaultAddress)
                .WithOne()
                .HasForeignKey<User>(u => u.DefaultAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict); //nie można usunąć Usera, jeśli ma zamowienia w systemie

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingMethod)
                .WithMany(sm => sm.Orders)
                .HasForeignKey(o => o.ShippingMethodId)
                .OnDelete(DeleteBehavior.Restrict); //nie można usunąć shippingMethod, jeśli istnieją zamówienia, które jej używają

            modelBuilder.Entity<Order>()
                .HasOne(o => o.PaymentMethod)
                .WithMany(pm => pm.Orders)
                .HasForeignKey(o => o.PaymentMethodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Size)
                .WithMany()
                .HasForeignKey(oi => oi.SizeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Size)
                .WithMany()
                .HasForeignKey(ci => ci.SizeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany()
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false); // Wskazuje, że UserId może być null

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Dodatkowe indeksy
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => ci.SessionId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();
        }
    }
}
