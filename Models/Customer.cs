
using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class Customer
    {
        public int Id { get; set; }

        // ===== User Relation =====
        public int? UserId { get; set; }

        public User? User { get; set; }

        // ===== Customer Info =====
        [Required(ErrorMessage = "Vui lòng nhập tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // ===== Navigation =====

        // 1 Customer -> nhiều Orders
        public List<Order> Orders { get; set; } = new();

        // 1 Customer -> nhiều Bookings
        public List<Booking> Bookings { get; set; } = new();

        // 1 Customer -> nhiều Reviews
     
        public List<Review> Reviews { get; set; } = new();

         public int AccumulatedPoints { get; set; } = 0;

        public int LoyaltyPoints { get; set; } = 0;
    }
}
