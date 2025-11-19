using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using static RegisterModel; // Dodaj na górze

public class LoginModel : PageModel
{
    [BindProperty]
    public string Username { get; set; }
    [BindProperty]
    public string Password { get; set; }
    public string ErrorMessage { get; set; }

    public void OnGet() { }


    public async Task<IActionResult> OnPostAsync()
    {
        if (Users.TryGetValue(Username, out var storedPassword) && storedPassword == Password)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Username)
        };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToPage("Index");
        }
        ErrorMessage = "Nieprawid³owa nazwa u¿ytkownika lub has³o.";
        return Page();
    }


}
