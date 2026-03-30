using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Models
{
    public enum CategoryStatus
    {
        Active,
        Inactive,
        Archived,
        Draft
    }

    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        [DisplayName("Category Name")]
        public string Name { get; set; }

        [MaxLength(500)]
        [DisplayName("Description")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [DisplayName("Status")]
        public CategoryStatus Status { get; set; } = CategoryStatus.Active;

        // SEO Fields
        [MaxLength(100)]
        [DisplayName("URL Slug")]
        public string? Slug { get; set; }

        [MaxLength(150)]
        [DisplayName("Meta Title")]
        public string? MetaTitle { get; set; }

        [MaxLength(300)]
        [DisplayName("Meta Description")]
        public string? MetaDescription { get; set; }

        // Image/Icon
        [MaxLength(200)]
        [DisplayName("Image URL")]
        public string? ImageUrl { get; set; }

        [MaxLength(50)]
        [DisplayName("Icon Class")]
        public string? IconClass { get; set; }

        // Display
        [DisplayName("Display Order")]
        [Range(0, 1000, ErrorMessage = "Order must be between 0 and 1000")]
        public int DisplayOrder { get; set; } = 0;

        // Audit Fields
        [DisplayName("Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [DisplayName("Updated At")]
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        [DisplayName("Created By")]
        public string? CreatedBy { get; set; }

        [MaxLength(100)]
        [DisplayName("Updated By")]
        public string? UpdatedBy { get; set; }

        // Soft Delete
        [DisplayName("Is Deleted?")]
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        [MaxLength(100)]
        public string? DeletedBy { get; set; }

        // Navigation
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        // Helper Method for Slug Generation
        public void GenerateSlug()
        {
            if (string.IsNullOrEmpty(Name)) return;

            Slug = Name.ToLower()
                      .Replace(" ", "-")
                      .Replace(".", "")
                      .Replace(",", "")
                      .Replace("&", "and");
        }
    }
}
