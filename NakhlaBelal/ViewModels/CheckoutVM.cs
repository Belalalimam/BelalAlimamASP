using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.ViewModels
{
    public class CheckoutVM
    {
        public CartVM CartData { get; set; } = new();


        // Cart Items
        public IEnumerable<Cart>? CartItems { get; set; }

        // Amounts
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Shipping Information
        [Required]
        [Display(Name = "First Name")]
        public string ShippingFirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string ShippingLastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string ShippingEmail { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone")]
        public string ShippingPhone { get; set; }

        [Required]
        [Display(Name = "Address")]
        public string ShippingAddress { get; set; }

        [Required]
        [Display(Name = "City")]
        public string ShippingCity { get; set; }

        [Required]
        [Display(Name = "State/Province")]
        public string ShippingState { get; set; }

        [Required]
        [Display(Name = "ZIP/Postal Code")]
        public string ShippingZipCode { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string ShippingCountry { get; set; } = "Egypt";

        // Billing Information
        public bool BillingSameAsShipping { get; set; } = true;

        [Display(Name = "First Name")]
        public string? BillingFirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? BillingLastName { get; set; }

        [Display(Name = "Address")]
        public string? BillingAddress { get; set; }

        [Display(Name = "City")]
        public string? BillingCity { get; set; }

        [Display(Name = "State/Province")]
        public string? BillingState { get; set; }

        [Display(Name = "ZIP/Postal Code")]
        public string? BillingZipCode { get; set; }

        [Display(Name = "Country")]
        public string? BillingCountry { get; set; }

        // Payment
        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Credit Card"; // Credit Card, Cash on Delivery, etc.

        // Notes
        [Display(Name = "Order Notes (optional)")]
        public string? CustomerNotes { get; set; }
    }
}
