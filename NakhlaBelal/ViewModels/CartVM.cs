using Stripe;

namespace NakhlaBelal.ViewModels
{
    public class CartVM
    {
        public List<Cart> CartItems { get; set; } = new();
        public Address? UserAddress { get; set; }

        // Totals
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
       public decimal Total { get; set; }


        // Promotion
        public string? PromotionCode { get; set; }
        public string? PromotionName { get; set; }


        // Add formatted properties for view
        public string FormattedSubtotal => Subtotal.ToString("N2");
        public string FormattedDiscount => Discount.ToString("N2");
        public string FormattedShipping => Shipping.ToString("N2");
        public string FormattedTax => Tax.ToString("N2");
        public string FormattedTotal => Total.ToString("N2");

        public string? ShippingAddress { get; set; }

        // Suggested products
        public List<Models.Product> SuggestedProducts { get; set; } = new();
    }
}
