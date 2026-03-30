namespace NakhlaBelal.Models
{
    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public int Rating { get; set; }
        public string ReviewText { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Product Product { get; set; }
    }
}
