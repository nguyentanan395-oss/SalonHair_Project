using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonHair.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int? BookingId { get; set; }
        public int? OrderId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; } = "";
        public string Status { get; set; } = "Chưa thanh toán";

        public string TransactionCode { get; set; } = "";
        public string? ProofImage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }

        public virtual Booking? Booking { get; set; }
        public virtual Order? Order { get; set; }
    }
}