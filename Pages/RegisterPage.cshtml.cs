using AlDentev2.Models;
using AlDentev2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;

namespace AlDentev2.Pages
{
    public class RegisterPageModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<RegisterPageModel> _logger;

        public RegisterPageModel(
            UserManager<User> userManager,
            IConfiguration configuration,
            IEmailSender emailSender,
            ILogger<RegisterPageModel> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public RegisterInput RegisterInput { get; set; } = new RegisterInput();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRegisterAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            _logger.LogInformation("RegisterInput: Email={Email}, Password={Password}, ConfirmPassword={ConfirmPassword}",
                RegisterInput.Email, RegisterInput.Password, RegisterInput.ConfirmPassword);

            foreach (var entry in ModelState)
            {
                _logger.LogInformation("ModelState Key: {Key}, Value: {Value}, Errors: {Errors}",
                    entry.Key,
                    entry.Value.RawValue,
                    string.Join("; ", entry.Value.Errors.Select(e => e.ErrorMessage)));
            }

            ModelState.Clear();

            if (string.IsNullOrEmpty(RegisterInput.Email))
            {
                ModelState.AddModelError("RegisterInput.Email", "Email jest wymagany");
            }
            if (!new EmailAddressAttribute().IsValid(RegisterInput.Email))
            {
                ModelState.AddModelError("RegisterInput.Email", "Nieprawid³owy format adresu email");
            }
            if (string.IsNullOrEmpty(RegisterInput.Password))
            {
                ModelState.AddModelError("RegisterInput.Password", "Has³o jest wymagane");
            }
            if (RegisterInput.Password != RegisterInput.ConfirmPassword)
            {
                ModelState.AddModelError("RegisterInput.ConfirmPassword", "Has³a nie s¹ jednakowe");
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Registration ModelState Error: {Error}", error.ErrorMessage);
                }
               
                return Page();
            }
            string recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
            string secretKey = _configuration["ReCaptcha:SecretKey"]!;
            string verificationUrl = _configuration["ReCaptcha:VerificationUrl"]!;
            bool isValid = await RecaptchaService.VerifyRecaptcha(recaptchaToken, secretKey, verificationUrl);
            if (!isValid)
            {
                StatusMessage = "Niepoprawna weryfikacja ReCaptcha";
                return Page();
            }
            var existingUser = await _userManager.FindByEmailAsync(RegisterInput.Email);
            if (existingUser != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(existingUser))
                {
                    ModelState.AddModelError(string.Empty, $"Konto z tym adresem email ju¿ istnieje, ale nie zosta³o potwierdzone. <a href='{Url.Page("/LoginPage", "ResendConfirmation", new { email = RegisterInput.Email })}'>Wyœlij ponownie link potwierdzaj¹cy</a>");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "U¿ytkownik z takim adresem email ju¿ istnieje");
                }
                _logger.LogWarning("Registration failed: Email {Email} already exists", RegisterInput.Email);

                return Page();
            }

            var user = new User
            {
                UserName = RegisterInput.Email,
                Email = RegisterInput.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, RegisterInput.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created successfully, awaiting email confirmation", RegisterInput.Email);

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/RegisterPage",
                    pageHandler: "ConfirmEmail",
                    values: new { userId = user.Id, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    RegisterInput.Email,
                    "PotwierdŸ swój adres e-mail",
                    $"<html><body style='font-family: Arial, sans-serif; color: #333;'><div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'><h2 style='color: #007bff;'>Witaj w AlDente!</h2><p>Dziêkujemy za rejestracjê. Proszê potwierdziæ swój adres e-mail, klikaj¹c w poni¿szy przycisk:</p><p style='text-align: center;'><a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display: inline-block; padding: 10px 20px; color: #fff; background-color: #007bff; text-decoration: none; border-radius: 5px;'>PotwierdŸ e-mail</a></p><p>Jeœli przycisk nie dzia³a, skopiuj i wklej ten link do przegl¹darki: <br>{HtmlEncoder.Default.Encode(callbackUrl)}</p><p>Pozdrawiamy,<br>Zespó³ AlDente</p></div></body></html>");

                StatusMessage = "Rejestracja zakoñczona sukcesem. SprawdŸ swoj¹ skrzynkê pocztow¹, aby potwierdziæ adres e-mail.";
         
                return Page();
            }

            foreach (var error in result.Errors)
            {
                string errorMessage = error.Code switch
                {
                    "PasswordRequiresDigit" => "Has³o musi zawieraæ co najmniej jedn¹ cyfrê ('0'-'9').",
                    "PasswordRequiresUpper" => "Has³o musi zawieraæ co najmniej jedn¹ wielk¹ literê ('A'-'Z').",
                    "PasswordTooShort" => "Has³o musi zawieraæ co najmniej 8 znaków.",
                    _ => error.Description
                };
                ModelState.AddModelError(string.Empty, errorMessage);
                _logger.LogError("Registration Identity Error: {Code} - {Description}", error.Code, error.Description);
            }
     
            return Page();
        }

        public async Task<IActionResult> OnGetConfirmEmailAsync(int userId, string code, string returnUrl = null)
        {
            _logger.LogInformation("ConfirmEmail called with UserId: {UserId}, Code: {Code}, ReturnUrl: {ReturnUrl}", userId, code, returnUrl);

            returnUrl ??= Url.Content("~/");
            if (userId == 0 || code == null)
            {
                StatusMessage = "B³¹d: Nieprawid³owy link potwierdzaj¹cy.";
                _logger.LogError("Invalid confirmation link: UserId={UserId}, Code={Code}", userId, code);
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                StatusMessage = "B³¹d: U¿ytkownik nie istnieje.";
                _logger.LogError("User not found for UserId: {UserId}", userId);
                return Page();
            }

            try
            {
                _logger.LogInformation("Attempting to decode token: {Code}", code);
                code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                _logger.LogInformation("Decoded token: {DecodedCode}", code);
            }
            catch (Exception ex)
            {
                _logger.LogError("Token decoding failed: {Error}", ex.Message);
                StatusMessage = "B³¹d: Nieprawid³owy lub uszkodzony token potwierdzaj¹cy.";
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for UserId: {UserId}, Email: {Email}", userId, user.Email);
                StatusMessage = "Adres e-mail zosta³ potwierdzony. Mo¿esz teraz siê zalogowaæ.";
                // Przekierowanie na stronê logowania z komunikatem
                return RedirectToPage("/LoginPage", new { statusMessage = StatusMessage });
            }

            foreach (var error in result.Errors)
            {
                _logger.LogError("Email Confirmation Error: {Code} - {Description}", error.Code, error.Description);
                ModelState.AddModelError(string.Empty, error.Description);
            }
            StatusMessage = "B³¹d podczas potwierdzania adresu e-mail: " + string.Join("; ", result.Errors.Select(e => e.Description));
            return Page();
        }

        public async Task<IActionResult> OnGetResendConfirmationAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                StatusMessage = "Podaj adres e-mail, aby wys³aæ ponownie link potwierdzaj¹cy.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
            {
                StatusMessage = "Jeœli konto istnieje, link potwierdzaj¹cy zostanie wys³any.";
                return Page();
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/RegisterPage", 
                pageHandler: "ConfirmEmail",
                values: new { userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "PotwierdŸ swój adres e-mail",
                $"<html><body style='font-family: Arial, sans-serif; color: #333;'><div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'><h2 style='color: #007bff;'>Witaj w AlDente!</h2><p>Dziêkujemy za rejestracjê. Proszê potwierdziæ swój adres e-mail, klikaj¹c w poni¿szy przycisk:</p><p style='text-align: center;'><a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display: inline-block; padding: 10px 20px; color: #fff; background-color: #007bff; text-decoration: none; border-radius: 5px;'>PotwierdŸ e-mail</a></p><p>Jeœli przycisk nie dzia³a, skopiuj i wklej ten link do przegl¹darki: <br>{HtmlEncoder.Default.Encode(callbackUrl)}</p><p>Pozdrawiamy,<br>Zespó³ AlDente</p></div></body></html>");

            StatusMessage = "Link potwierdzaj¹cy zosta³ wys³any na podany adres e-mail.";
            return Page();
        }

    }
}
