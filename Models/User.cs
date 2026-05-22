using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tài khoản không được để trống")]
        [Display(Name = "Tài khoản")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public required string Password { get; set; }

        public int RoleId { get; set; } = 1; // 1: Customer, 2: Admin
        public Role? Role { get; set; }

        public string? Language { get; set; } = "vi";

        public string? OtpCode { get; set; }
        public DateTime? OtpExpiryTime { get; set; }
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public bool IsEmailVerified { get; set; } = false;
    }
}
    