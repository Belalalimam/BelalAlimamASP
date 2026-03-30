using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Models
{
    public class ProjectCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; }
        public string? Icon { get; set; }
        [MaxLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
