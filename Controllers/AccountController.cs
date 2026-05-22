using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;
using SalonHair.Models.SalonHair.Models;
using SalonHair.Services;
using System.Security.Claims;

namespace SalonHair.Controllers
{
    public class AccountController : Controller
    {
        private readonly SalonContext _context;
        private readonly IEmailService _emailService;

        public AccountController(SalonContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(User user)
        {
            var existingUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == user.Username);

            if (existingUser != null)
            {
                bool isPasswordValid = false;
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password);
                }
                catch
                {
                    // Hỗ trợ đăng nhập cho các tài khoản cũ chưa mã hóa
                    isPasswordValid = (user.Password == existingUser.Password);
                }

                if (isPasswordValid)
                {
                    if (!existingUser.IsEmailVerified && existingUser.RoleId != 2)
                    {
                        ModelState.AddModelError("", "Tài khoản chưa được xác thực email. Vui lòng kiểm tra email của bạn hoặc đăng ký lại.");
                        return View(user);
                    }

                    // Sign in
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, existingUser.Username),
                        new Claim(ClaimTypes.Role, existingUser.Role?.RoleName ?? "Customer")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    if (existingUser.RoleId == 2) // Admin
                    {
                        return RedirectToAction("Index", "Bookings");
                    }
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng.");
            return View(user);
        }

        [HttpGet]
        public IActionResult VerifyOtp(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Username = username;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string username, string otpCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Check if OTP is correct and not expired
            if (user.OtpCode == otpCode && user.OtpExpiryTime.HasValue && user.OtpExpiryTime.Value > DateTime.UtcNow)
            {
                // Clear OTP and set IsEmailVerified
                user.OtpCode = null;
                user.OtpExpiryTime = null;
                user.IsEmailVerified = true;
                await _context.SaveChangesAsync();

                // Redirect to Login
                TempData["SuccessMessage"] = "Xác thực tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ViewBag.Username = username;
            ModelState.AddModelError("", "Mã xác thực không hợp lệ hoặc đã hết hạn.");
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("", "Tên tài khoản đã tồn tại.");
                    return View(user);
                }

                user.RoleId = 1; // Mặc định là khách hàng (Customer)
                user.IsEmailVerified = false;
                
                // Mã hóa mật khẩu trước khi lưu
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                
                // 1. Generate 6-digit OTP
                var random = new Random();
                var otp = random.Next(100000, 999999).ToString();

                // 2. Set OTP in user object
                user.OtpCode = otp;
                user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(5);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 3. Send email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendOtpEmailAsync(user.Email, otp);
                }

                return RedirectToAction("VerifyOtp", new { username = user.Username });
            }
            return View(user);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == User.Identity.Name);

            if (user == null) return NotFound();
            return View(user);
        }
    }
}
