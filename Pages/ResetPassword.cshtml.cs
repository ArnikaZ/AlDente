using AlDentev2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace AlDentev2.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(UserManager<User> userManager, ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public bool IsValidToken { get; set; } = true;
        public bool IsSuccess { get; set; } = false;

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            public int UserId { get; set; }

            [Required]
            public string Code { get; set; } = string.Empty;

            [Required(ErrorMessage = "Has�o jest wymagane")]
            [StringLength(100, ErrorMessage = "Has�o musi zawiera� co najmniej {2} znak�w", MinimumLength = 8)]
            [RegularExpression(@"^(?=.*\d)(?=.*[A-Z])(?=.*[@#$%^&+=!]).{8,}$", ErrorMessage = "Has�o musi zawiera� co najmniej jedn� cyfr�, jedn� wielk� liter� i jeden znak specjalny (@#$%^&+=!)")]
            [DataType(DataType.Password)]
            [Display(Name = "Nowe has�o")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Potwierdzenie has�a jest wymagane")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Has�a nie s� jednakowe")]
            [Display(Name = "Potwierd� has�o")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(int userId, string code)
        {
            _logger.LogInformation("OnGetAsync: Received UserId={UserId}, Code={Code}", userId, code);
            if (string.IsNullOrEmpty(code) || userId == 0)
            {
                IsValidToken = false;
                StatusMessage = "Nieprawid�owy link do resetowania has�a.";
                _logger.LogWarning("Nieprawid�owy link do resetowania has�a: UserId={UserId}, Code={Code}", userId, code ?? "null");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                IsValidToken = false;
                StatusMessage = "Nie znaleziono u�ytkownika.";
                _logger.LogWarning("Nie znaleziono u�ytkownika dla UserId={UserId}", userId);
                return Page();
            }

            Input.UserId = userId;
            Input.Code = code;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("OnPostAsync: UserId={UserId}, Code={Code}", Input.UserId, Input.Code);
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId.ToString());
            if (user == null)
            {
                IsValidToken = false;
                StatusMessage = "Nie znaleziono u�ytkownika.";
                _logger.LogWarning("Nie znaleziono u�ytkownika dla UserId={UserId}", Input.UserId);
                return Page();
            }

            try
            {
                // Decode the token to handle potential double-encoding
                var decodedCode = HttpUtility.UrlDecode(Input.Code);
                _logger.LogInformation("Decoded token: {DecodedCode}", decodedCode);

                var result = await _userManager.ResetPasswordAsync(user, decodedCode, Input.Password);
                if (result.Succeeded)
                {
                    IsSuccess = true;
                    StatusMessage = "Has�o zosta�o zresetowane. Mo�esz teraz si� zalogowa�.";
                    _logger.LogInformation("Has�o zresetowane dla u�ytkownika: {UserId}", Input.UserId);
                    return Page();
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    _logger.LogWarning("B��d resetowania has�a: {Error}", error.Description);
                }
                IsValidToken = false;
                StatusMessage = "Nieprawid�owy lub wygas�y link do resetowania has�a.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "B��d podczas resetowania has�a dla u�ytkownika: {UserId}", Input.UserId);
                StatusMessage = "Wyst�pi� b��d podczas resetowania has�a. Spr�buj ponownie.";
            }

            return Page();
        }
    }
}
