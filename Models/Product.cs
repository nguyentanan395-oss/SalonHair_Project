using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonHair.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Display(Name = "Tên sản phẩm")]
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Giá")]
        [Required(ErrorMessage = "Vui lòng nhập giá")]
        public double Price { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        // ===== Navigation =====

        // 1 Product -> nhiều OrderDetails
        public List<OrderDetail> OrderDetails { get; set; } = new();

        // 1 Product -> nhiều Reviews
        public List<Review> Reviews { get; set; } = new();
    }
}
