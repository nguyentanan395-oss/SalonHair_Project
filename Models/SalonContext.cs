namespace SalonHair.Models
{
    using Microsoft.EntityFrameworkCore;

    namespace SalonHair.Models
    {
        public class SalonContext : DbContext
        {
            public SalonContext(DbContextOptions<SalonContext> options)
                : base(options)
            {
            }

            // public DbSet<Customer> Customers { get; set; }
            // public DbSet<Hairstyle> Hairstyles { get; set; }
            // public DbSet<Service> Services { get; set; }
            // public DbSet<Booking> Bookings { get; set; }
            // public DbSet<Product> Products { get; set; }
            // public DbSet<Order> Orders { get; set; }
            // public DbSet<OrderDetail> OrderDetails { get; set; }
            // public DbSet<User> Users { get; set; }
            // public DbSet<Review> Reviews { get; set; }
            // public DbSet<Role> Roles { get; set; }
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

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<Role>().HasData(
                    new Role { RoleId = 1, RoleName = "Customer" },
                    new Role { RoleId = 2, RoleName = "Admin" }
                );
            }
        }
    }
}


