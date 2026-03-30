using Microsoft.EntityFrameworkCore;

namespace NakhlaBelal.Models
{
    [PrimaryKey(nameof(ApplicationUserId), nameof(ProductId))]
    public class Cart
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ApplicationUserId { get; set; }
        public int Count { get; set; } = 1;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public Product? Product { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }
}
