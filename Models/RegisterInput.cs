using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Models
{
    public class RegisterInput
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        [StringLength(50, ErrorMessage = "Hasło musi zawierać co najmniej {2} znaków", MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Hasła nie są jednakowe")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
