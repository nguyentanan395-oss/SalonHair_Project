using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class Hairstyle
    {
        public int Id { get; set; }

        [Required]
        public string StyleName { get; set; }

        public string? ImageUrl { get; set; }

        public string? FaceShape { get; set; }

        public string? Description { get; set; }
    }
}
