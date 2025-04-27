using AlDentev2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Pages
{
    public class EditProfileModel : PageModel
    {
        private readonly UserManager<User> _userManager;

        public EditProfileModel(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [StringLength(50, ErrorMessage = "Imi� nie mo�e przekracza� 50 znak�w")]
            public string? FirstName { get; set; }

            [StringLength(50, ErrorMessage = "Nazwisko nie mo�e przekracza� 50 znak�w")]
            public string? LastName { get; set; }

            [Phone(ErrorMessage = "Nieprawid�owy format numeru telefonu")]
            [StringLength(15, ErrorMessage = "Numer telefonu nie mo�e przekracza� 15 znak�w")]
            public string? PhoneNumber { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return RedirectToPage("/LoginPage");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Update user properties
            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.PhoneNumber = Input.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = "Dane zosta�y zaktualizowane pomy�lnie.";
                return RedirectToPage("/CustomerProfile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            StatusMessage = "Wyst�pi� b��d podczas aktualizacji danych.";
            return Page();
        }
    }
}
