using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace AlDentev2.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName ="decimal(18,2)")]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }
        public ICollection<ProductImage>? ProductImages { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? SKU { get; set; } //kod produktu

        public ICollection<ProductSize>? ProductSizes { get; set; }
       
    }
    public class ProductSize
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int SizeId { get; set; }
        public Size? Size { get; set; }

        [Required]
        public int StockQuantity { get; set; }
    }
    public class Size
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string Name { get; set; } = string.Empty; // S, M, L, XL 

        public ICollection<ProductSize>? ProductSizes { get; set; }
    }
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;


        public ICollection<Product>? Products { get; set; }
    }

}
