using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations.Schema;

namespace NakhlaBelal.Models
{
    public class Product
    {
        public int Id { get; set; }

        // Basic Information
        public string Name { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string SKU { get; set; } = GenerateSKU();
        public string? Barcode { get; set; }
        public string? ModelNumber { get; set; }

        // Pricing - FIXED: All decimal
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal? SpecialPrice { get; set; }
        public DateTime? SpecialPriceStart { get; set; }
        public DateTime? SpecialPriceEnd { get; set; }
        public decimal Discount { get; set; } // CHANGED: double → decimal

        // Inventory
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 10;
        public bool ManageStock { get; set; } = true;
        public bool IsInStock { get; set; } = true;
        public bool BackordersAllowed { get; set; } = false;


        // Properties
        


        // Media
        public string MainImage { get; set; }

        // Store GalleryImages as JSON string (since List<string> can't be stored directly)
        public string? GalleryImagesJson { get; set; }

        public string? VideoUrl { get; set; }

        // SEO & URLs
        public string Slug { get; set; }
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }

        // Dimensions & Weight
        public decimal? Weight { get; set; } // kg
        public decimal? Length { get; set; } // cm
        public decimal? Width { get; set; } // cm
        public decimal? Height { get; set; } // cm

        // Display & Features
        public bool Status { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public bool IsNew { get; set; } = true;
        public bool IsBestSeller { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public int ViewsCount { get; set; } = 0;
        public int Traffic { get; set; } = 0;
        public int Rate { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        public decimal AverageRating { get; set; } = 0;

        // Relationships
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int BrandId { get; set; }
        public Brand? Brand { get; set; }

        public int? ColorId { get; set; }
        [ForeignKey("ColorId")]
        public Color? Color { get; set; }


        // Specifications (JSON or separate table)
        public string? Specifications { get; set; } // JSON string

        // Additional Features
        public bool HasVariations { get; set; } = false;
        public bool IsDownloadable { get; set; } = false;
        public bool IsVirtual { get; set; } = false;
        public bool RequiresShipping { get; set; } = true;
        public bool Taxable { get; set; } = true;
        public string? TaxClass { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "System";
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        // Navigation Properties
        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
        public ICollection<ProductAttribute> ProductAttributes { get; set; } = new List<ProductAttribute>();
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<ProductComposition> ProductCompositions { get; set; } = new List<ProductComposition>();

        public int? FabricTypeId { get; set; }
        public FabricType? FabricType { get; set; } = null!;
        public ICollection<ProjectCategory> ProjectCategories { get; set; } = new List<ProjectCategory>();




        // Calculated Properties
        [NotMapped]
        public decimal FinalPrice
        {
            get
            {
                // Check for active special price
                if (SpecialPrice.HasValue &&
                    (!SpecialPriceStart.HasValue || DateTime.Now >= SpecialPriceStart.Value) &&
                    (!SpecialPriceEnd.HasValue || DateTime.Now <= SpecialPriceEnd.Value))
                {
                    return SpecialPrice.Value;
                }

                // Apply discount
                if (Discount > 0)
                {
                    return Price - (Price * Discount / 100);
                }

                return Price;
            }
        }

        [NotMapped]
        public bool IsOnSale => FinalPrice < Price;

        [NotMapped]
        public bool IsLowStock => ManageStock && StockQuantity <= LowStockThreshold && StockQuantity > 0;

        [NotMapped]
        public bool IsOutOfStock => ManageStock && StockQuantity <= 0;

        [NotMapped]
        public List<string> GalleryImages
        {
            get
            {
                if (string.IsNullOrEmpty(GalleryImagesJson))
                    return new List<string>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<string>>(GalleryImagesJson)
                        ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                GalleryImagesJson = System.Text.Json.JsonSerializer.Serialize(value ?? new List<string>());
            }
        }

        // Helper method to generate SKU
        private static string GenerateSKU()
        {
            return "PROD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        // Constructor to set defaults
        public Product()
        {
            // Set defaults in constructor
            Status = true;
            IsFeatured = false;
            IsNew = true;
            IsBestSeller = false;
            DisplayOrder = 0;
            StockQuantity = 0;
            LowStockThreshold = 10;
            ManageStock = true;
            IsInStock = true;
            BackordersAllowed = false;
            Discount = 0;
            ViewsCount = 0;
            Rate = 0;
            ReviewCount = 0;
            AverageRating = 0;
            Traffic = 0;
            HasVariations = false;
            IsDownloadable = false;
            IsVirtual = false;
            RequiresShipping = true;
            Taxable = true;
            IsDeleted = false;
            CreatedAt = DateTime.Now;
            CreatedBy = "System";
        }
    }
}
