using System.ComponentModel.DataAnnotations.Schema;

namespace NakhlaBelal.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser? User { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
