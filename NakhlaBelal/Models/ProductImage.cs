namespace NakhlaBelal.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public string? AltText { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; }
        public string Img { get; set; } = string.Empty;
    }
}
