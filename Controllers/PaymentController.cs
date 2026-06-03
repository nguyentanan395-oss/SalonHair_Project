using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SalonHair.Models;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace SalonHair.Controllers
{
    public class PaymentController : Controller
    {
        private readonly SalonContext _context;

        public PaymentController(SalonContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
{
    var payments = await _context.Payments
        .Include(p => p.Booking)
        .Include(p => p.Order)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    return View(payments);
}

        [Authorize]
        public async Task<IActionResult> History()
        {
            var payments = await _context.Payments
                .Include(p => p.Booking)
                .Include(p => p.Order)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            payment.Status = "Đã thanh toán";
            payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult ProcessPayment(int bookingId, decimal amount, string method)
        {
            var payment = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                Method = method,
                Status = method == "Cash" ? "Chờ thanh toán tại salon" : "Đang xử lý",
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();

            if (method == "PayOS")
            {
                return RedirectToAction("PayOSMock", new { bookingId = bookingId, amount = amount });
            }
            else if (method == "QRBanking")
            {
                return RedirectToAction("QrPayment", new { bookingId = bookingId });
            }

            // Tiền mặt (Cash) - Cập nhật trạng thái Booking nếu model hỗ trợ
            var booking = _context.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking != null)
            {
                // booking.Status = "Confirmed"; // TODO: Mở comment khi Booking model có trường Status
                _context.SaveChanges();
            }

            return RedirectToAction("PaymentSuccess");
        }

        public IActionResult PayOSMock(int bookingId, decimal amount)
        {
            ViewBag.BookingId = bookingId;
            ViewBag.Amount = amount;
            return View();
        }


        public IActionResult QrPayment(int bookingId)
        {
            ViewBag.BookingId = bookingId;
            return View();
        }

        public IActionResult VNPayMock(int bookingId, decimal amount)
        {
            ViewBag.BookingId = bookingId;
            ViewBag.Amount = amount;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadProof(
            IFormFile imageFile,
            int bookingId)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                // Tạo tên file ngẫu nhiên
                var fileName = Guid.NewGuid().ToString() +
                               Path.GetExtension(imageFile.FileName);

                // Đường dẫn thư mục lưu ảnh
                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "payments");

                // Tạo folder nếu chưa có
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Đường dẫn file
                var filePath = Path.Combine(uploadFolder, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Tìm payment theo booking
                var payment = _context.Payments
                    .FirstOrDefault(x => x.BookingId == bookingId);

                // Nếu chưa có payment thì tạo mới
                if (payment == null)
                {
                    payment = new Payment
                    {
                        BookingId = bookingId,
                        Status = "Pending"
                    };

                    _context.Payments.Add(payment);
                }

                // Cập nhật thông tin ảnh
                payment.ProofImage = "/uploads/payment/" + fileName;
                payment.Status = "Pending";

                // Lưu database
                _context.SaveChanges();
            }

            return RedirectToAction("PaymentSuccess");
        }

        public IActionResult PaymentSuccess()
        {
            return View();
        }
    }
}