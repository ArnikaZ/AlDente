using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace AlDentev2.Models
{
    public class User: IdentityUser<int>
    {
      

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Adres domyślny użytkownika
        public int? DefaultAddressId { get; set; }
        public Address? DefaultAddress { get; set; }

        // Kolekcja wszystkich adresów użytkownika
        public ICollection<Address>? Addresses { get; set; }

        // Kolekcja zamówień użytkownika
        public ICollection<Order>? Orders { get; set; }
    }
    public class Address
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [StringLength(50)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(50)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Country { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;
    }
}
