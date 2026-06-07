using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;

namespace SalonHair.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly SalonContext _context;

        public UsersController(SalonContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search)
        {
            var users = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    u.Username.Contains(search) ||
                    u.Email.Contains(search));
            }

            ViewBag.Search = search;
            ViewBag.Roles = await _context.Roles.ToListAsync();

            return View(await users.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                ModelState.AddModelError("", "Tên tài khoản đã tồn tại.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError("", "Email đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(user);
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.IsEmailVerified = true;
            user.IsLocked = false;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    email = u.Email,
                    roleId = u.RoleId,
                    language = u.Language
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Json(user);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy tài khoản."
                });
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != id))
            {
                return Json(new
                {
                    success = false,
                    message = "Email đã tồn tại trong hệ thống."
                });
            }

            user.Username = model.Username;
            user.Email = model.Email;
            user.RoleId = model.RoleId;
            user.Language = model.Language;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Cập nhật tài khoản thành công."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUnlock(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsLocked = !user.IsLocked;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Customer != null)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản vì tài khoản này đang được liên kết với khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa tài khoản thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}