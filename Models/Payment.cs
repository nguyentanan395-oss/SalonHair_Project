using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SalonHair.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public int BookingId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; } = "";

        public string Status { get; set; } = "";

        public string TransactionCode { get; set; } = "";

        public string? ProofImage { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
