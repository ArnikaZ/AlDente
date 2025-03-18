using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        // Koszyk może być przypisany do użytkownika (gdy zalogowany)
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Lub do sesji (gdy niezalogowany)
        public string? SessionId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int SizeId { get; set; }
        public Size? Size { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
