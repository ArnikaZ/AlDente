using AlDentev2.Models;
using AlDentev2.Repositories;
using AlDentev2.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginPageModel> _logger;

        public LoginPageModel(SignInManager<User> signInManager, UserManager<User> userManager, ICartRepository cartRepository, IConfiguration configuration, ILogger<LoginPageModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _cartRepository = cartRepository;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        public LoginInput LoginInput { get; set; } = new LoginInput();


        [TempData]
        public string? StatusMessage { get; set; }

   

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

            var user = await _userManager.FindByEmailAsync(LoginInput.Email);
            if (user != null && !await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Proszê potwierdziæ swój adres e-mail przed zalogowaniem.");
                _logger.LogWarning("Login failed: Email not confirmed for {Email}", LoginInput.Email);
             
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

            
            else if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Konto jest zablokowane");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Nieprawid³owy email lub has³o");
            }
            _logger.LogWarning("Failed login attempt for {Email}", LoginInput.Email);
        
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
                
                return Page();
            }

            // No existing login, create or associate account
            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                StatusMessage = "Nie uda³o siê pobraæ adresu e-mail od dostawcy zewnêtrznego.";
           
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    StatusMessage = $"Konto z tym adresem email ju¿ istnieje, ale nie zosta³o potwierdzone. <a href='{Url.Page("/LoginPage", "ResendConfirmation", new { email })}'>Wyœlij ponownie link potwierdzaj¹cy</a>";
             
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
      
            return Page();
        }

       

    }


}

