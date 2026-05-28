using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonHair.Models
{
    public class Booking
    {
        public int Id { get; set; }

        // ===== FK mới =====
        [Display(Name = "Khách hàng")]
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        // ===== Giữ field cũ để tránh lỗi =====
        [Display(Name = "Tên khách hàng")]
        [Required(ErrorMessage = "Vui lòng nhập tên")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        // ===== Service =====
        [Display(Name = "Dịch vụ")]
        public int ServiceId { get; set; }

        public Service? Service { get; set; }

        // ===== Thời gian đặt =====
        [Display(Name = "Ngày đặt lịch")]
        public DateTime BookingDate { get; set; }

        // ===== Có thể thêm sau =====
        // public string Status { get; set; } = "Pending";
        // public string Notes { get; set; } = string.Empty;

        public virtual Payment? Payment { get; set; }
    }
}
