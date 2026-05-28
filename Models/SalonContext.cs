
using Microsoft.EntityFrameworkCore;

namespace SalonHair.Models
{
    public class SalonContext : DbContext
    {
        public SalonContext(DbContextOptions<SalonContext> options)
            : base(options)
        {
        }

        // ===== DbSet =====

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Hairstyle> Hairstyles { get; set; }

        public DbSet<Service> Services { get; set; }

        public DbSet<Booking> Bookings { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<Role> Roles { get; set; }
        
        public DbSet<Payment> Payments { get; set; }

        // ===== Relation =====

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Role Seed =====

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Customer" },
                new Role { RoleId = 2, RoleName = "Admin" }
            );

            // ===== User - Role =====

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== User - Customer (1-1) =====

            modelBuilder.Entity<User>()
                .HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Customer - Orders =====

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Customer - Bookings =====

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Service - Bookings =====

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Order - OrderDetails =====

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Product - OrderDetails =====

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Customer - Reviews =====

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Product - Reviews =====

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Service - Reviews =====

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Service)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Hairstyle - Reviews =====

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Hairstyle)
                .WithMany(h => h.Reviews)
                .HasForeignKey(r => r.HairstyleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }
}
