using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonHair.Models;
using SalonHair.Models;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SalonHair.Controllers
{
    public class LanguageController : Controller
    {
        private readonly SalonContext _context;

        public LanguageController(SalonContext context)
        {
            _context = context;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetLanguage(string lang, string returnUrl)
        {
            // Set google translation cookie directly in header
            Response.Cookies.Append("googtrans", $"/vi/{lang}", new CookieOptions 
            { 
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/"
            });

            // Save to DB for authenticated users
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    user.Language = lang;
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true });
        }
    }
}
