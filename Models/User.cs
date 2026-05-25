
using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class User
    {
        public int Id { get; set; }

        // ===== Username =====
        [Required(ErrorMessage = "Tài khoản không được để trống")]
        [Display(Name = "Tài khoản")]
        public string Username { get; set; } = string.Empty;

        // ===== Password =====
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        // ===== Role =====
        public int RoleId { get; set; } = 1;

        public Role? Role { get; set; }

        // ===== Language =====
        public string? Language { get; set; } = "vi";

        // ===== OTP =====
        public string? OtpCode { get; set; }

        public DateTime? OtpExpiryTime { get; set; }

        // ===== Email =====
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        // ===== Verify =====
        public bool IsEmailVerified { get; set; } = false;

        // ===== Navigation =====

        // 1 User -> 1 Customer
        public Customer? Customer { get; set; }
    }
}
