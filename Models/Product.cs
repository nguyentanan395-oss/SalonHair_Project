using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Display(Name = "Tên sản phẩm")]
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public required string Name { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Giá")]
        [Required(ErrorMessage = "Vui lòng nhập giá")]
        public double Price { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }
    }
}
