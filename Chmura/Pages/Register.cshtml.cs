using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;

public class RegisterModel : PageModel
{
    [BindProperty]
    public string Username { get; set; }
    [BindProperty]
    public string Password { get; set; }
    public string ErrorMessage { get; set; }
    public string SuccessMessage { get; set; }

    // Prosta "baza" u¿ytkowników w pamiêci (do testów)
    public static ConcurrentDictionary<string, string> Users = new();

    public void OnGet() { }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Podaj nazwê użytkownika i hasło.";
            return Page();
        }

        if (Users.ContainsKey(Username))
        {
            ErrorMessage = "Użytkownik o tej nazwie już istnieje.";
            return Page();
        }

        Users[Username] = Password; // W praktyce has³o powinno byæ hashowane!
        SuccessMessage = "Rejestracja zakończona sukcesem. Możesz się zalogować.";
        return Page();
    }
}