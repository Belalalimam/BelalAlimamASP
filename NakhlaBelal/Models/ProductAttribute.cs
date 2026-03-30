namespace NakhlaBelal.Models
{
    public class ProductAttribute
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string AttributeName { get; set; } // e.g., "Color", "Size"
        public string AttributeValue { get; set; } // e.g., "Red", "XL"
        public decimal? PriceAdjustment { get; set; }
        public int? StockAdjustment { get; set; }
        public Product Product { get; set; }
    }
}
