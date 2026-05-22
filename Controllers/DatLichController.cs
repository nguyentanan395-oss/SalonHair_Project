using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SalonHair.Models;
using SalonHair.Models.SalonHair.Models;
using System.Linq;

namespace SalonHair.Controllers
{
    [Authorize]
    public class DatLichController : Controller
    {
        private readonly SalonContext _context;

        public DatLichController(SalonContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.ServiceId = new SelectList(_context.Services, "Id", "ServiceName");
            return View();
        }

        [HttpPost]
        public IActionResult Index(Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Bookings.Add(booking);
                _context.SaveChanges();
                // Lưu vào Session và chuyển hướng về trang Lịch sử
                HttpContext.Session.SetString("LastPhone", booking.Phone);
                return RedirectToAction("History", "Bookings", new { phone = booking.Phone });
            }

            ViewBag.ServiceId = new SelectList(_context.Services, "Id", "ServiceName");
            return View();
        }
    }
}