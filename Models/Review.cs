
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonHair.Models
{
    public class Review
    {
        public int Id { get; set; }

        // ===== Customer =====
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        // ===== Product =====
        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        // ===== Service =====
        public int? ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        // ===== Hairstyle AI =====
        public int? HairstyleId { get; set; }

        [ForeignKey("HairstyleId")]
        public Hairstyle? Hairstyle { get; set; }

        // ===== Giữ field cũ =====
        [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = string.Empty;

        // ===== Rating =====
        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá (Sao)")]
        public int Rating { get; set; }

        // ===== Comment =====
        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [Display(Name = "Nội dung góp ý")]
        public string Comment { get; set; } = string.Empty;

        // ===== Created =====
        [Display(Name = "Ngày gửi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}