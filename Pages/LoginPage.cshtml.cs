using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;

namespace AlDentev2.Pages
{
    public class LoginPageModel : PageModel
    {
        private readonly IUserRepository _userRepository;
        private readonly ICartRepository _cartRepository;
        public LoginPageModel(IUserRepository userRepository, ICartRepository cartRepository)
        {
            _userRepository = userRepository;
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
            if (!ModelState.IsValid) //jeœli formularz nie jest wype³niony poprawnie
            {
                return Page();
            }
            var user = await _userRepository.GetUserByEmailAsync(LoginInput.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Nieprawid³owy email lub has³o");
                return Page();
            }
            if(!VerifyPasswordHash(LoginInput.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Nieprawid³owy email lub has³o");
                return Page();
            }
            await SignInUserAsync(user);

            string? sessionId = HttpContext.Session.Id;
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _cartRepository.TransferCartAsync(sessionId, user.Id);
            }
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var existingUser = await _userRepository.GetUserByEmailAsync(RegisterInput.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "U¿ytkownik z takim adresem email ju¿ istnieje");
                return Page();
            }
            if (RegisterInput.Password != RegisterInput.ConfirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Has³a musz¹ siê zgadzaæ");
                return Page();
            }
            var user = new User
            {
                Email = RegisterInput.Email,
                PasswordHash = HashPassword(RegisterInput.Password),
                CreatedAt=DateTime.UtcNow
            };
            await _userRepository.CreateUserAsync(user);
            await SignInUserAsync(user);

            string? sessionId = HttpContext.Session.Id;
            if (!string.IsNullOrEmpty(sessionId))
            {
                await _cartRepository.TransferCartAsync(sessionId, user.Id);
            }
            StatusMessage = "Zarejestrowano pomyœlnie";
            return RedirectToPage("/Index");
        }

        private async Task SignInUserAsync(User user)
        {
            //u¿yæ ASP.NET core identity
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
        private bool VerifyPasswordHash(string password, string passwordHash)
        {
            return password == passwordHash;
        }

    }
}
