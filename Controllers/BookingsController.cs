using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalonHair.Controllers
{
    public class BookingsController : Controller
    {
        private readonly SalonContext _context;

        public BookingsController(SalonContext context)
        {
            _context = context;
        }

        // GET: Bookings
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            // Phải có .ToListAsync() để gửi về một danh sách
            var list = await _context.Bookings.Include(b => b.Service).ToListAsync();
            return View(list);
        }

        // GET: Bookings/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Bookings/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName");
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerName,Phone,ServiceId,BookingDate")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", booking.ServiceId);
            return View(booking);
        }

        // GET: Bookings/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound();
            }

            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", booking.ServiceId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerName,Phone,ServiceId,BookingDate")] Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", booking.ServiceId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        private async Task<Customer?> GetCurrentCustomerAsync(string? phone = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userName = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }

            var user = await _context.Users
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.Username == userName);

            if (user == null)
            {
                return null;
            }

            if (user.Customer != null)
            {
                var customer = user.Customer;
                if (!string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(customer.Phone))
                {
                    customer.Phone = phone;
                    await _context.SaveChangesAsync();
                }
                return customer;
            }

            var createdCustomer = new Customer
            {
                UserId = user.Id,
                Name = user.Username,
                Email = user.Email,
                Phone = phone ?? string.Empty
            };

            _context.Customers.Add(createdCustomer);
            await _context.SaveChangesAsync();
            return createdCustomer;
        }

        // 1. Trang hiển thị Form cho khách nhập (GET)
        [AllowAnonymous]
        public IActionResult DatLich()
        {
            // Lấy danh sách dịch vụ từ Database để đổ vào Dropdown
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName");
            return View();
        }

        // 2. Xử lý khi khách bấm nút "Đặt lịch" (POST)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLich([Bind("CustomerName,Phone,ServiceId,BookingDate")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                var currentCustomer = await GetCurrentCustomerAsync(booking.Phone);
                if (currentCustomer != null)
                {
                    booking.CustomerId = currentCustomer.Id;
                    booking.CustomerName = string.IsNullOrWhiteSpace(booking.CustomerName)
                        ? currentCustomer.Name
                        : booking.CustomerName;
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("LastPhone", booking.Phone);
                TempData["SuccessMessage"] = "Lịch hẹn của bạn đã được ghi nhận. Salon sẽ sớm liên hệ để xác nhận.";
                return RedirectToAction("History");
            }
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "ServiceName", booking.ServiceId);
            return View(booking);
        }

        // 3. Trang lịch sử dành cho khách (GET)
        [AllowAnonymous]
        public async Task<IActionResult> History(string? phone)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
            ViewBag.UserName = User.Identity?.Name;

            if (ViewBag.IsAuthenticated)
            {
                var user = await _context.Users
                    .Include(u => u.Customer)
                    .FirstOrDefaultAsync(u => u.Username == User.Identity!.Name);

                if (user?.Customer == null)
                {
                    return View(Array.Empty<Booking>());
                }

                var bookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Where(b => b.CustomerId == user.Customer.Id)
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                var orders = await _context.Orders
                    .Where(o => o.CustomerId == user.Customer.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                ViewBag.Orders = orders;
                ViewBag.Phone = user.Customer.Phone;
                return View(bookings);
            }

            if (string.IsNullOrEmpty(phone))
            {
                phone = HttpContext.Session.GetString("LastPhone");
            }

            if (string.IsNullOrEmpty(phone))
            {
                return View();
            }

            HttpContext.Session.SetString("LastPhone", phone);
            ViewBag.Phone = phone;
            var guestBookings = await _context.Bookings
                .Include(b => b.Service)
                .Where(b => b.Phone == phone)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var guestOrders = await _context.Orders
                .Where(o => o.Phone == phone)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Orders = guestOrders;
            return View(guestBookings);
        }

        // 4. Trang thống kê tổng quan hệ thống (Dashboard) dành cho Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TodayBookings = await _context.Bookings.CountAsync(b => b.BookingDate.Date == DateTime.Today);

            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.TotalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalServices = await _context.Services.CountAsync();
            ViewBag.TotalCustomers = await _context.Users.CountAsync(u => u.RoleId == 1);

            ViewBag.AvgRating = await _context.Reviews.AnyAsync() ? await _context.Reviews.AverageAsync(r => (double)r.Rating) : 0;
            ViewBag.TotalReviews = await _context.Reviews.CountAsync();

            return View();
        }
    }
}
