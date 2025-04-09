using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Models
{
    public class LoginInput
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
