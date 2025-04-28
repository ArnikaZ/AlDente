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
            [Required(ErrorMessage = "Imi� i nazwisko s� wymagane")]
            [StringLength(100, ErrorMessage = "Imi� i nazwisko nie mog� przekracza� 100 znak�w")]
            [Display(Name = "Imi� i nazwisko")]
            public string NameSurname { get; set; } = string.Empty;

            [Required(ErrorMessage = "Adres e-mail jest wymagany")]
            [EmailAddress(ErrorMessage = "Nieprawid�owy format adresu e-mail")]
            [Display(Name = "Adres e-mail")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Temat jest wymagany")]
            [StringLength(100, ErrorMessage = "Temat nie mo�e przekracza� 100 znak�w")]
            [Display(Name = "Temat")]
            public string Topic { get; set; } = string.Empty;

            [Required(ErrorMessage = "Tre�� wiadomo�ci jest wymagana")]
            [StringLength(1000, ErrorMessage = "Tre�� wiadomo�ci nie mo�e przekracza� 1000 znak�w")]
            [Display(Name = "Tre�� wiadomo�ci")]
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
                var htmlMessage = $@"<h3>Nowa wiadomo�� z formularza kontaktowego</h3>
                                <p><strong>Imi� i nazwisko:</strong> {Input.NameSurname}</p>
                                <p><strong>Email:</strong> {Input.Email}</p>
                                <p><strong>Temat:</strong> {Input.Topic}</p>
                                <p><strong>Wiadomo��:</strong><br>{Input.ContactText}</p>";

                await _emailSender.SendEmailAsync(
                    "aldentebrand@gmail.com",
                    $"Wiadomo�� od {Input.NameSurname}: {Input.Topic}",
                    htmlMessage
                );

                StatusMessage = "Wiadomo�� zosta�a wys�ana pomy�lnie. Dzi�kujemy za kontakt!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                StatusMessage = "Wyst�pi� b��d podczas wysy�ania wiadomo�ci. Spr�buj ponownie p�niej.";
                // Optionally log the error (e.g., using ILogger)
                return Page();
            }
        }

    }
}
