using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AlDentev2.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public int ShippingAddressId { get; set; }
        public Address? ShippingAddress { get; set; }

        [Required]
        public int ShippingMethodId { get; set; }
        public ShippingMethod? ShippingMethod { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        // Pozycje zamówienia
        public ICollection<OrderItem>? OrderItems { get; set; }
    }
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int SizeId { get; set; }
        public Size? Size { get; set; }

        [Required]
        public int Quantity { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }

    public class ShippingMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName="decimal(18,2)")]
        public decimal Cost { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public ICollection<Order>? Orders { get; set; }

    }

    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public ICollection<Order>? Orders { get; set; }
    }

}
