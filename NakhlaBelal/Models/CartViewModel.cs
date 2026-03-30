namespace NakhlaBelal.Models
{
    public class CartViewModel
    {
        public int id { get; set; }
        public List<CartItemViewModel> CartItems { get; set; } = new();
        public List<RelatedProductViewModel> RelatedProducts { get; set; } = new();
        public List<PromoProductViewModel> PromoProducts { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Duties { get; set; }
        public decimal Total { get; set; }
        public decimal USDAmount { get; set; }
        public string ShippingAddress { get; set; } = "848 Oak Road, Animi exercitatione, Dolore ut cupidatat harum, Egypt, 86462, French Polynesia";
        public string CouponCode { get; set; }
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string ImageUrl { get; set; }
        public decimal PricePerUnit { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public string Unit { get; set; } = "m"; // meters for fabrics
    }

    public class RelatedProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool IsOnSale { get; set; }
    }

    public class PromoProductViewModel
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
    }
}
