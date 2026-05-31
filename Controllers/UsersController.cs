// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using SalonHair.Models;
// using SalonHair.Models;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;

// namespace SalonHair.Controllers
// {
//     // Only admins can access this controller
//     [Authorize(Roles = "Admin")]
//     public class UsersController : Controller
//     {
//         private readonly SalonContext _context;

//         public UsersController(SalonContext context)
//         {
//             _context = context;
//         }

//         // GET: /Users
//         public async Task<IActionResult> Index()
//         {
//             // Load all users with their role
//             var users = await _context.Users.Include(u => u.Role).ToListAsync();
//             return View(users);
//         }

//         // GET: /Users/Edit/5
//         public async Task<IActionResult> Edit(int id)
//         {
//             var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
//             if (user == null)
//             {
//                 return NotFound();
//             }
//             // Pass the list of roles for dropdown
//             ViewBag.Roles = await _context.Roles.ToListAsync();
//             return View(user);
//         }

//         // POST: /Users/Edit/5
//         [HttpPost]
//         [ValidateAntiForgeryToken]
//         public async Task<IActionResult> Edit(int id, int roleId)
//         {
//             var user = await _context.Users.FindAsync(id);
//             if (user == null)
//             {
//                 return NotFound();
//             }
//             // Update role
//             user.RoleId = roleId;
//             _context.Update(user);
//             await _context.SaveChangesAsync();
//             return RedirectToAction(nameof(Index));
//         }
//     }
// }
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

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User model)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != id))
            {
                ModelState.AddModelError("", "Email đã tồn tại trong hệ thống.");
                ViewBag.Roles = await _context.Roles.ToListAsync();
                return View(model);
            }

            user.Username = model.Username;
            user.Email = model.Email;
            user.RoleId = model.RoleId;
            user.Language = model.Language;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUnlock(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound();

            user.IsLocked = !user.IsLocked;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}