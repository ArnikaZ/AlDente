using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public bool IsMain { get; set; } = false;
    }
}
