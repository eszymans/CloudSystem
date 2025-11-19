using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chmura.Pages
{
    [AllowAnonymous]
    public class HomeModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
