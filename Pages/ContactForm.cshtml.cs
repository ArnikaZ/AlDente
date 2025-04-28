using AlDentev2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Pages
{
    public class ContactFormModel : PageModel
    {
        private readonly IEmailSender _emailSender;
        public ContactFormModel(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        [BindProperty]
        public ContactInputModel Input { get; set; } = new ContactInputModel();

        [TempData]
        public string StatusMessage { get; set; }

        public class ContactInputModel
        {
            [Required(ErrorMessage = "Imiê i nazwisko s¹ wymagane")]
            [StringLength(100, ErrorMessage = "Imiê i nazwisko nie mog¹ przekraczaæ 100 znaków")]
            [Display(Name = "Imiê i nazwisko")]
            public string NameSurname { get; set; } = string.Empty;

            [Required(ErrorMessage = "Adres e-mail jest wymagany")]
            [EmailAddress(ErrorMessage = "Nieprawid³owy format adresu e-mail")]
            [Display(Name = "Adres e-mail")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Temat jest wymagany")]
            [StringLength(100, ErrorMessage = "Temat nie mo¿e przekraczaæ 100 znaków")]
            [Display(Name = "Temat")]
            public string Topic { get; set; } = string.Empty;

            [Required(ErrorMessage = "Treœæ wiadomoœci jest wymagana")]
            [StringLength(1000, ErrorMessage = "Treœæ wiadomoœci nie mo¿e przekraczaæ 1000 znaków")]
            [Display(Name = "Treœæ wiadomoœci")]
            public string ContactText { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            // Initialize the page, no action needed
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var htmlMessage = $@"<h3>Nowa wiadomoœæ z formularza kontaktowego</h3>
                                <p><strong>Imiê i nazwisko:</strong> {Input.NameSurname}</p>
                                <p><strong>Email:</strong> {Input.Email}</p>
                                <p><strong>Temat:</strong> {Input.Topic}</p>
                                <p><strong>Wiadomoœæ:</strong><br>{Input.ContactText}</p>";

                await _emailSender.SendEmailAsync(
                    "aldentebrand@gmail.com",
                    $"Wiadomoœæ od {Input.NameSurname}: {Input.Topic}",
                    htmlMessage
                );

                StatusMessage = "Wiadomoœæ zosta³a wys³ana pomyœlnie. Dziêkujemy za kontakt!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                StatusMessage = "Wyst¹pi³ b³¹d podczas wysy³ania wiadomoœci. Spróbuj ponownie póŸniej.";
                // Optionally log the error (e.g., using ILogger)
                return Page();
            }
        }

    }
}
