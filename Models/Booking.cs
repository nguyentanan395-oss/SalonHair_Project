using System;
using System.ComponentModel.DataAnnotations;

namespace SalonHair.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public string CustomerName { get; set; }

        public string Phone { get; set; }

        public int ServiceId { get; set; }

        public DateTime BookingDate { get; set; }

        // Liên kết với bảng Service
        public Service? Service { get; set; }
    }
}