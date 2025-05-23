using AlDentev2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlDentev2.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<User> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Usunięcie danych sesji po stronie serwera
            HttpContext.Session.Clear();

            // Usunięcie cookie sesyjnego z przeglądarki
            Response.Cookies.Delete(".AspNetCore.Session");

            // Wylogowanie użytkownika
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Użytkownik wylogowany, sesja unieważniona.");

            return RedirectToPage("/Index");
        }
    }
}
