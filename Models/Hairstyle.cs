using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class Hairstyle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên kiểu tóc")]
        [Display(Name = "Tên kiểu tóc")]
        public string StyleName { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Dáng khuôn mặt")]
        public string? FaceShape { get; set; }

        // GIỚI TÍNH
        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        // THÊM MỚI
        [Display(Name = "Độ tuổi")]
        public string? AgeGroup { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // ===== Navigation =====

        // 1 Hairstyle -> nhiều Reviews
        public List<Review> Reviews { get; set; } = new();
    }
}