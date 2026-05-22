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

        // GET: Reviews (Dành cho Admin quản lý)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(reviews);
        }

        // GET: Reviews/Create (Dành cho khách hàng)
        [AllowAnonymous]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Reviews/Create
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerName,Rating,Comment")] Review review)
        {
            if (ModelState.IsValid)
            {
                review.CreatedAt = DateTime.Now;
                _context.Add(review);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Success));
            }
            return View(review);
        }

        [AllowAnonymous]
        public IActionResult Success()
        {
            return View();
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null) _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}