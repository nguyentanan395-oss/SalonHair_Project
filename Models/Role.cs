using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Phải có thư viện này

namespace SalonHair.Models
{
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Chặn tự tăng, bạn tự điền 1, 2 theo ý mình
        public int RoleId { get; set; }

        [Required]
        [MaxLength(64)]
        public string RoleName { get; set; } = string.Empty;

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}