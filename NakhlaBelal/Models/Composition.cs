using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Models
{
    public class Composition
    {
        public int Id { get; set; }
        public string Name { get; set; } // مثال: Cotton

        [MaxLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}
