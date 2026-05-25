using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SalonHair.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }

        // ===== Order =====
        public int OrderId { get; set; }

        public Order? Order { get; set; }

        // ===== Product =====
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        // ===== Quantity =====
        public int Quantity { get; set; }

        // ===== Price tại thời điểm mua =====
        public double Price { get; set; }
    }
}
