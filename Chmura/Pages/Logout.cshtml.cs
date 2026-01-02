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

        public async Task<IActionResult> OnGetAsync()
        {
            var username = User.Identity?.Name ?? "anonim";
            await _loggingService.LogLogoutAsync(username);
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync("Google");
            
            return Redirect("/");
        }
    }
}
