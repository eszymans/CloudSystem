using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Chmura.Services;

namespace Chmura.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly ILoggingService _loggingService;

        public LogoutModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public async Task<IActionResult> OnGet()
        {
            var username = User.Identity?.Name ?? "anonim";

            // Zaloguj wylogowanie PRZED wylogowaniem
            await _loggingService.LogLogoutAsync(username);

            // Wyloguj siê
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Home");
        }
    }
}
