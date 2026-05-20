using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
        [Display(Name = "Tên khách hàng")]
        public required string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá")]
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao")]
        [Display(Name = "Đánh giá (Sao)")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [Display(Name = "Nội dung góp ý")]
        public required string Comment { get; set; }

        [Display(Name = "Ngày gửi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
