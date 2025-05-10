using AlDentev2.Models;
using AlDentev2.Repositories;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AlDentev2.Pages
{
    public class LoginPageModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ICartRepository _cartRepository;
        private readonly Services.IEmailSender _emailSender;
        private readonly ILogger<LoginPageModel> _logger;
        

        public LoginPageModel(SignInManager<User> signInManager, UserManager<User> userManager, ICartRepository cartRepository, Services.IEmailSender emailSender, ILogger<LoginPageModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _cartRepository = cartRepository;
            _emailSender = emailSender;
            _logger = logger;
            
        }

        [BindProperty]
        public LoginInput LoginInput { get; set; } = new LoginInput();

        [BindProperty]
        public RegisterInput RegisterInput { get; set; } = new RegisterInput();

       

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "login";

        public void OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect("/");
            }
        }

        public async Task<IActionResult> OnPostLoginAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            _logger.LogInformation("LoginInput: Email={Email}, Password={Password}, RememberMe={RememberMe}",
                LoginInput.Email, LoginInput.Password, LoginInput.RememberMe);

            foreach (var entry in ModelState)
            {
                _logger.LogInformation("ModelState Key: {Key}, Value: {Value}, Errors: {Errors}",
                    entry.Key,
                    entry.Value.RawValue,
                    string.Join("; ", entry.Value.Errors.Select(e => e.ErrorMessage)));
            }

            ModelState.Clear();

            if (string.IsNullOrEmpty(LoginInput.Email))
            {
                ModelState.AddModelError("LoginInput.Email", "Email jest wymagany");
            }
            if (!new EmailAddressAttribute().IsValid(LoginInput.Email))
            {
                ModelState.AddModelError("LoginInput.Email", "Nieprawid³owy format adresu email");
            }
            if (string.IsNullOrEmpty(LoginInput.Password))
            {
                ModelState.AddModelError("LoginInput.Password", "Has³o jest wymagane");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login attempt: ModelState errors");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Login ModelState Error: {Error}", error.ErrorMessage);
                }
                ActiveTab = "login";
                return Page();
            }

           

            var user = await _userManager.FindByEmailAsync(LoginInput.Email);
            if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Proszê potwierdziæ swój adres e-mail przed zalogowaniem.");
                _logger.LogWarning("Login failed: Email not confirmed for {Email}", LoginInput.Email);
                ActiveTab = "login";
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(LoginInput.Email, LoginInput.Password, LoginInput.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                if (user != null)
                {
                    string? sessionId = HttpContext.Session.Id;
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await _cartRepository.TransferCartAsync(sessionId, user.Id);
                    }
                }
                _logger.LogInformation("User {Email} logged in successfully", LoginInput.Email);
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                ModelState.AddModelError(string.Empty, "Wymagane jest uwierzytelnianie dwusk³adnikowe");
            }
            else if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Konto jest zablokowane");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Nieprawid³owy email lub has³o");
            }
            _logger.LogWarning("Failed login attempt for {Email}", LoginInput.Email);
            ActiveTab = "login";
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
                ActiveTab = "register";
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
                ActiveTab = "register";
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
                    "/LoginPage",
                    pageHandler: "ConfirmEmail",
                    values: new { userId = user.Id, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    RegisterInput.Email,
                    "PotwierdŸ swój adres e-mail",
                    $"<html><body style='font-family: Arial, sans-serif; color: #333;'><div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'><h2 style='color: #007bff;'>Witaj w AlDente!</h2><p>Dziêkujemy za rejestracjê. Proszê potwierdziæ swój adres e-mail, klikaj¹c w poni¿szy przycisk:</p><p style='text-align: center;'><a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display: inline-block; padding: 10px 20px; color: #fff; background-color: #007bff; text-decoration: none; border-radius: 5px;'>PotwierdŸ e-mail</a></p><p>Jeœli przycisk nie dzia³a, skopiuj i wklej ten link do przegl¹darki: <br>{HtmlEncoder.Default.Encode(callbackUrl)}</p><p>Pozdrawiamy,<br>Zespó³ AlDente</p></div></body></html>");

                StatusMessage = "Rejestracja zakoñczona sukcesem. SprawdŸ swoj¹ skrzynkê pocztow¹, aby potwierdziæ adres e-mail.";
                ActiveTab = "login";
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
            ActiveTab = "register";
            return Page();
        }
        

        public async Task<IActionResult> OnGetConfirmEmailAsync(int userId, string code, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (userId == 0 || code == null)
            {
                StatusMessage = "B³¹d: Nieprawid³owy link potwierdzaj¹cy.";
                ActiveTab = "login";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                StatusMessage = "B³¹d: U¿ytkownik nie istnieje.";
                ActiveTab = "login";
                return Page();
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                StatusMessage = "Adres e-mail zosta³ potwierdzony. Mo¿esz teraz siê zalogowaæ.";
                ActiveTab = "login";
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                _logger.LogError("Email Confirmation Error: {Code} - {Description}", error.Code, error.Description);
            }
            StatusMessage = "B³¹d podczas potwierdzania adresu e-mail.";
            ActiveTab = "login";
            return Page();
        }

        public async Task<IActionResult> OnGetResendConfirmationAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                StatusMessage = "Podaj adres e-mail, aby wys³aæ ponownie link potwierdzaj¹cy.";
                ActiveTab = "login";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
            {
                StatusMessage = "Jeœli konto istnieje, link potwierdzaj¹cy zostanie wys³any.";
                ActiveTab = "login";
                return Page();
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/LoginPage",
                pageHandler: "ConfirmEmail",
                values: new { userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "PotwierdŸ swój adres e-mail",
                $"<html><body style='font-family: Arial, sans-serif; color: #333;'><div style='max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'><h2 style='color: #007bff;'>Witaj w AlDente!</h2><p>Dziêkujemy za rejestracjê. Proszê potwierdziæ swój adres e-mail, klikaj¹c w poni¿szy przycisk:</p><p style='text-align: center;'><a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display: inline-block; padding: 10px 20px; color: #fff; background-color: #007bff; text-decoration: none; border-radius: 5px;'>PotwierdŸ e-mail</a></p><p>Jeœli przycisk nie dzia³a, skopiuj i wklej ten link do przegl¹darki: <br>{HtmlEncoder.Default.Encode(callbackUrl)}</p><p>Pozdrawiamy,<br>Zespó³ AlDente</p></div></body></html>");

            StatusMessage = "Link potwierdzaj¹cy zosta³ wys³any na podany adres e-mail.";
            ActiveTab = "login";
            return Page();
        }

        public IActionResult OnPostExternalLogin(string provider, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            HttpContext.Session.SetString("TestKey", "TestValue"); // Test zapisu sesji
            var testValue = HttpContext.Session.GetString("TestKey");
            _logger.LogInformation("Session test: {Value}", testValue);

            var redirectUrl = Url.Page("/LoginPage", pageHandler: "ExternalLoginCallback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetExternalLoginCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            _logger.LogInformation("External login callback called. Full URL: {Url}, ReturnUrl: {ReturnUrl}, RemoteError: {RemoteError}, Session ID: {SessionId}, Query: {Query}",
                Request.GetDisplayUrl(), returnUrl, remoteError, HttpContext.Session.Id, Request.QueryString);

            if (remoteError != null)
            {
                _logger.LogError("External provider error: {Error}", remoteError);
                StatusMessage = $"B³¹d zewnêtrznego dostawcy: {remoteError}";
                ActiveTab = "login";
                return Page();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Failed to retrieve external login info. Session ID: {SessionId}, Request Headers: {Headers}, Query Parameters: {Query}",
                    HttpContext.Session.Id,
                    string.Join("; ", Request.Headers.Select(h => $"{h.Key}: {h.Value}")),
                    string.Join("; ", Request.Query.Select(q => $"{q.Key}: {q.Value}")));
                StatusMessage = "B³¹d podczas ³adowania informacji o logowaniu zewnêtrznym.";
                ActiveTab = "login";
                return Page();
            }

            _logger.LogInformation("External login info retrieved. Provider: {Provider}, ProviderKey: {ProviderKey}", info.LoginProvider, info.ProviderKey);
            // Try to sign in with existing external login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Provider} provider.", info.LoginProvider);
                var user1 = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user1 != null)
                {
                    string? sessionId = HttpContext.Session.Id;
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await _cartRepository.TransferCartAsync(sessionId, user1.Id);
                    }
                }
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                StatusMessage = "Konto jest zablokowane.";
                ActiveTab = "login";
                return Page();
            }

            // No existing login, create or associate account
            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                StatusMessage = "Nie uda³o siê pobraæ adresu e-mail od dostawcy zewnêtrznego.";
                ActiveTab = "login";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    StatusMessage = $"Konto z tym adresem email ju¿ istnieje, ale nie zosta³o potwierdzone. <a href='{Url.Page("/LoginPage", "ResendConfirmation", new { email })}'>Wyœlij ponownie link potwierdzaj¹cy</a>";
                    ActiveTab = "login";
                    return Page();
                }

                // Associate external login with existing user
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    string? sessionId = HttpContext.Session.Id;
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await _cartRepository.TransferCartAsync(sessionId, user.Id);
                    }
                    _logger.LogInformation("Associated {Provider} login with existing user {Email}.", info.LoginProvider, email);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                StatusMessage = "B³¹d podczas ³¹czenia konta zewnêtrznego.";
                ActiveTab = "login";
                return Page();
            }

            // Create new user
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true, // External provider verifies email
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    string? sessionId = HttpContext.Session.Id;
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        await _cartRepository.TransferCartAsync(sessionId, user.Id);
                    }
                    _logger.LogInformation("Created and logged in new user {Email} with {Provider} provider.", email, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            StatusMessage = "B³¹d podczas tworzenia konta zewnêtrznego.";
            ActiveTab = "login";
            return Page();
        }

    }
}
