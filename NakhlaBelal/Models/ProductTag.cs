using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Models
{
    public class ProductTag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        [MaxLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
