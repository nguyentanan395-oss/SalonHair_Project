
using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ")]
        [Display(Name = "Tên dịch vụ")]
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Display(Name = "Giá")]
        public double Price { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        // ===== Navigation =====

        // 1 Service -> nhiều Bookings
        public List<Booking> Bookings { get; set; } = new();

        // 1 Service -> nhiều Reviews
        public List<Review> Reviews { get; set; } = new();
    }
}
