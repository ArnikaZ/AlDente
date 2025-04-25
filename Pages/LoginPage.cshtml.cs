using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;

namespace AlDentev2.Pages
{
    public class LoginPageModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ICartRepository _cartRepository;

        public LoginPageModel(SignInManager<User> signInManager, UserManager<User> userManager, ICartRepository cartRepository)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _cartRepository = cartRepository;
        }

        [BindProperty]
        public LoginInput LoginInput { get; set; } = new LoginInput();

        [BindProperty]
        public RegisterInput RegisterInput { get; set; } = new RegisterInput();

        [TempData]
        public string? StatusMessage { get; set; }

        public void OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect("/");
            }
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(LoginInput.Email, LoginInput.Password, LoginInput.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(LoginInput.Email);
                if (user != null)
                {
                    string? sessionId = HttpContext.Session.Id;
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await _cartRepository.TransferCartAsync(sessionId, user.Id);
                    }
                }
                return RedirectToPage("/Index");
            }

            ModelState.AddModelError(string.Empty, "Nieprawid³owy email lub has³o");
            return Page();
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new User
            {
                UserName = RegisterInput.Email, // Identity uses UserName for login, set it to Email
                Email = RegisterInput.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, RegisterInput.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                string? sessionId = HttpContext.Session.Id;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    await _cartRepository.TransferCartAsync(sessionId, user.Id);
                }
                StatusMessage = "Zarejestrowano pomyœlnie";
                return RedirectToPage("/Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

    }
}
