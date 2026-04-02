using System.ComponentModel.DataAnnotations.Schema;

namespace NakhlaBelal.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // Foreign Keys
        public int OrderId { get; set; }
        public int ProductId { get; set; }

        // Item Details
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } // Price at time of purchase

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Product snapshot (in case product details change later)
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }
        public string? ProductSku { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}
