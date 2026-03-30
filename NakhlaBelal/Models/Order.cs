using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NakhlaBelal.Models
{
    public class Order
    {
        public int Id { get; set; }

        // Order Information
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = GenerateOrderNumber();

        // Customer Information
        [Required]
        public string ApplicationUserId { get; set; } // ADD THIS LINE

        // Shipping Information
        [StringLength(100)]
        public string ShippingFirstName { get; set; }

        [StringLength(100)]
        public string ShippingLastName { get; set; }

        [StringLength(200)]
        public string ShippingAddress { get; set; }

        [StringLength(100)]
        public string ShippingCity { get; set; }

        [StringLength(100)]
        public string ShippingState { get; set; }

        [StringLength(20)]
        public string ShippingZipCode { get; set; }

        [StringLength(100)]
        public string ShippingCountry { get; set; } = "Egypt";

        [StringLength(20)]
        public string ShippingPhone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string ShippingEmail { get; set; }

        // Billing Information (can be same as shipping)
        [StringLength(100)]
        public string BillingFirstName { get; set; }

        [StringLength(100)]
        public string BillingLastName { get; set; }

        [StringLength(200)]
        public string BillingAddress { get; set; }

        [StringLength(100)]
        public string BillingCity { get; set; }

        [StringLength(100)]
        public string BillingState { get; set; }

        [StringLength(20)]
        public string BillingZipCode { get; set; }

        [StringLength(100)]
        public string BillingCountry { get; set; } = "Egypt";

        // Order Details
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Payment Information
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Credit Card"; // Cash, Credit Card, PayPal, etc.

        [StringLength(100)]
        public string? PaymentTransactionId { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed, Refunded

        // Order Status
        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

        public DateTime? ProcessingDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? CancelledDate { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        // Shipping Information
        [StringLength(100)]
        public string? ShippingMethod { get; set; } // Standard, Express, Overnight

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        [StringLength(100)]
        public string? Carrier { get; set; } // FedEx, UPS, DHL, etc.

        // Promotions
        [StringLength(50)]
        public string? PromotionCode { get; set; }

        // Notes
        [StringLength(1000)]
        public string? CustomerNotes { get; set; }

        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation Properties
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? ApplicationUser { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Helper method to generate order number
        public static string GenerateOrderNumber()
        {
            return "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }

        // Alternative with sequential number (better for order tracking):
        public static string GenerateOrderNumber(int lastOrderNumber = 0)
        {
            // If you want sequential numbers: ORD-20240115-0001, ORD-20240115-0002, etc.
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var sequentialNumber = (lastOrderNumber + 1).ToString("D4"); // 4-digit padding
            return $"ORD-{datePart}-{sequentialNumber}";
        }

        // Calculated Properties
        [NotMapped]
        public string FullName => $"{ShippingFirstName} {ShippingLastName}";

        [NotMapped]
        public string BillingFullName => $"{BillingFirstName} {BillingLastName}";

        [NotMapped]
        public string FormattedAddress => $"{ShippingAddress}, {ShippingCity}, {ShippingState} {ShippingZipCode}, {ShippingCountry}";

        [NotMapped]
        public string FormattedBillingAddress => $"{BillingAddress}, {BillingCity}, {BillingState} {BillingZipCode}, {BillingCountry}";

        [NotMapped]
        public bool IsPaid => PaymentStatus == "Paid";

        [NotMapped]
        public bool IsPending => OrderStatus == "Pending";

        [NotMapped]
        public bool IsShipped => OrderStatus == "Shipped";

        [NotMapped]
        public bool IsDelivered => OrderStatus == "Delivered";

        [NotMapped]
        public bool IsCancelled => OrderStatus == "Cancelled";
    }
}
