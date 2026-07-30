// Models/Order.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WonderlandBackend.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        // ✅ Payment fields
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? PaymentStatus { get; set; } // "Paid", "Unpaid", "Failed"
        public DateTime? PaymentDate { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}