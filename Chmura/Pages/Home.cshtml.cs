using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chmura.Pages
{
    [AllowAnonymous]
    public class HomeModel : PageModel
    {
        public void OnGet()
        {
            // Jeśli zalogowany, idź na Index
            if (User.Identity?.IsAuthenticated == true)
            {
                // Automatyczne przekierowanie jest w Home.cshtml
            }
        }

        public IActionResult OnPost()
        {
            // Bez logowania tutaj! Challenge() jeszcze nie ma użytkownika
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Index")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}
