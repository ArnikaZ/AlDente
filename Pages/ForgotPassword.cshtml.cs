using AlDentev2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;

namespace AlDentev2.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly Services.IEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<User> userManager, Services.IEmailSender emailSender, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required(ErrorMessage = "Email jest wymagany")]
            [EmailAddress(ErrorMessage = "Nieprawid³owy format adresu email")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                StatusMessage = "Jeœli podany adres e-mail jest powi¹zany z kontem, wys³ano link do resetowania has³a.";
                _logger.LogInformation("Próba resetowania has³a dla nieistniej¹cego lub niepotwierdzonego emaila: {Email}", Input.Email);
                return RedirectToPage();
            }

            try
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                _logger.LogInformation("Generated reset token for user {UserId}: {Token}", user.Id, code); // Log raw token
                code = UrlEncoder.Default.Encode(code);
                _logger.LogInformation("Encoded reset token: {EncodedToken}", code); // Log encoded token
                var callbackUrl = Url.Page(
                    "/ResetPassword",
                    pageHandler: null,
                    values: new { userId = user.Id, code = code },
                    protocol: Request.Scheme);
                _logger.LogInformation("Callback URL: {CallbackUrl}", callbackUrl); // Log full URL

                var htmlMessage = $@"<p>Otrzymaliœmy proœbê o zresetowanie has³a dla Twojego konta.</p>
                            <p>Kliknij poni¿szy link, aby ustawiæ nowe has³o:</p>
                            <p><a href='{callbackUrl}'>Zresetuj has³o</a></p>
                            <p>Link jest wa¿ny przez 24 godziny. Jeœli nie prosi³eœ o reset has³a, zignoruj tê wiadomoœæ.</p>";

                await _emailSender.SendEmailAsync(Input.Email, "Resetowanie has³a - Al Dente", htmlMessage);
                StatusMessage = "Jeœli podany adres e-mail jest powi¹zany z kontem, wys³ano link do resetowania has³a.";
                _logger.LogInformation("Wys³ano email z linkiem do resetowania has³a dla u¿ytkownika: {Email}", Input.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B³¹d podczas wysy³ania emaila z linkiem do resetowania has³a dla: {Email}", Input.Email);
                StatusMessage = "Wyst¹pi³ b³¹d podczas wysy³ania linku. Spróbuj ponownie póŸniej.";
                return Page();
            }

            return RedirectToPage();
        }
    }
}
