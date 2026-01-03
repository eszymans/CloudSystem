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
            if (User.Identity?.IsAuthenticated == true)
            {
            }
        }

        public IActionResult OnPost()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Page("/Index")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
    }
}
