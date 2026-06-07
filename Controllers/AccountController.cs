using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;
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
                // Kiểm tra tài khoản có bị khóa không
                if (existingUser.IsLocked)
                {
                    ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                    return View(user);
                }
                
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


        [HttpGet]
            public IActionResult ForgotPassword()
            {
                return View();
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ForgotPassword(string email)
            {
                if (string.IsNullOrEmpty(email))
                {
                    ModelState.AddModelError("", "Vui lòng nhập email.");
                    return View();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Email không tồn tại trong hệ thống.");
                    return View();
                }

                var random = new Random();
                var otp = random.Next(100000, 999999).ToString();

                user.OtpCode = otp;
                user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(5);

                await _context.SaveChangesAsync();

                await _emailService.SendOtpEmailAsync(user.Email, otp);

                return RedirectToAction("ResetPassword", new { email = user.Email });
            }

            [HttpGet]
            public IActionResult ResetPassword(string email)
            {
                if (string.IsNullOrEmpty(email))
                {
                    return RedirectToAction("ForgotPassword");
                }

                ViewBag.Email = email;
                return View();
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string otpCode, string newPassword, string confirmPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại.");
                ViewBag.Email = email;
                return View();
            }

            if (user.OtpCode != otpCode || !user.OtpExpiryTime.HasValue || user.OtpExpiryTime.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Mã OTP không đúng hoặc đã hết hạn.");
                ViewBag.Email = email;
                return View();
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu mới phải từ 6 ký tự trở lên.");
                ViewBag.Email = email;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                ViewBag.Email = email;
                return View();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.OtpCode = null;
            user.OtpExpiryTime = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
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

            // Lấy thông tin khách hàng tương ứng để hiển thị Họ tên
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            ViewBag.FullName = customer?.Name;

            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string email, string language)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email không được để trống.";
                return RedirectToAction(nameof(Profile));
            }

            user.Email = email;
            user.Language = language;

            // Đồng bộ Họ và tên sang bảng Customers
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer != null)
            {
                customer.Name = fullName;
                _context.Update(customer);
            }
            
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null) return NotFound();

            // Kiểm tra mật khẩu hiện tại
            bool isPasswordValid = false;
            try {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.Password);
            } catch {
                isPasswordValid = (currentPassword == user.Password);
            }

            if (!isPasswordValid)
            {
                TempData["PasswordError"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction(nameof(Profile));
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["PasswordError"] = "Mật khẩu mới phải từ 6 ký tự trở lên.";
                return RedirectToAction(nameof(Profile));
            }

            if (newPassword != confirmPassword)
            {
                TempData["PasswordError"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction(nameof(Profile));
            }

            // Mã hóa mật khẩu mới
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(Profile));
        }
    }
}
