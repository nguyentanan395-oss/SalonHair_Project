using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonHair.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [ForeignKey("Booking")]
        public int BookingId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; } = "";

        public string Status { get; set; } = "";

        public string TransactionCode { get; set; } = "";

        public string? ProofImage { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual Booking? Booking { get; set; }
    }
}