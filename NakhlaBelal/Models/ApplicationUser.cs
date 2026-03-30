using Microsoft.AspNetCore.Identity;

namespace NakhlaBelal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
    }
}
