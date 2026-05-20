using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;
using SalonHair.Models.SalonHair.Models;

namespace SalonHair.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly SalonContext _context;

        public ReviewsController(SalonContext context)
        {
            _context = context;
        }

        // KHÁCH HÀNG: Tạo đánh giá mới
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerName,Rating,Comment")] Review review)
        {
            if (ModelState.IsValid)
            {
                review.CreatedAt = DateTime.Now;
                _context.Add(review);
                await _context.SaveChangesAsync();
                return View("Success");
            }
            return View(review);
        }

        // ADMIN: Xem danh sách đánh giá
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(reviews);
        }

        // ADMIN: Xóa đánh giá
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
