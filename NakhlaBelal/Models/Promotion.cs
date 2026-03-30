using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NakhlaBelal.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        // Basic Information
        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        // Discount Configuration
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Range(0, 100)]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumPurchaseAmount { get; set; }

        // Usage Limits
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;
        public int? UsageLimitPerCustomer { get; set; }

        // Validity Period
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(1);

        // Eligibility
        public bool IsValid { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool ApplyToAllProducts { get; set; } = true;
        public bool ApplyToShipping { get; set; } = false;
        public bool ApplyToTax { get; set; } = false;

        // Target Products/Categories
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }

        // Customer Restrictions
        public bool ForNewCustomersOnly { get; set; } = false;
        public bool ForExistingCustomersOnly { get; set; } = false;
        public decimal? CustomerMinOrderCount { get; set; }
        public decimal? CustomerMinPurchaseAmount { get; set; }

        // Combinability
        public bool CanCombineWithOtherPromotions { get; set; } = false;
        public string? ExcludedPromotions { get; set; } // JSON array of promotion IDs

        // Display Settings
        public string? BannerImage { get; set; }
        public string? BannerText { get; set; }
        public string? BadgeColor { get; set; } = "#FF0000";
        public bool ShowOnHomepage { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = "System";
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation Properties
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [ForeignKey("BrandId")]
        public Brand? Brand { get; set; }

        // NotMapped Properties
        [NotMapped]
        public bool IsExpired => DateTime.Now > EndDate;

        [NotMapped]
        public bool IsStarted => DateTime.Now >= StartDate;

        [NotMapped]
        public bool IsCurrentlyActive => IsActive && IsValid && IsStarted && !IsExpired && (UsageLimit == null || UsedCount < UsageLimit);

        [NotMapped]
        public bool IsFullyUsed => UsageLimit.HasValue && UsedCount >= UsageLimit.Value;

        [NotMapped]
        public TimeSpan RemainingTime => EndDate - DateTime.Now;

        [NotMapped]
        public List<int> ExcludedPromotionIds
        {
            get
            {
                if (string.IsNullOrEmpty(ExcludedPromotions))
                    return new List<int>();

                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<int>>(ExcludedPromotions)
                        ?? new List<int>();
                }
                catch
                {
                    return new List<int>();
                }
            }
            set
            {
                ExcludedPromotions = System.Text.Json.JsonSerializer.Serialize(value ?? new List<int>());
            }
        }

        // Methods
        public decimal CalculateDiscount(decimal originalPrice, int quantity = 1)
        {
            if (!IsCurrentlyActive)
                return 0;

            decimal discount = 0;

            switch (DiscountType)
            {
                case DiscountType.Percentage:
                    discount = originalPrice * (DiscountValue / 100);
                    if (MaximumDiscountAmount.HasValue && discount > MaximumDiscountAmount.Value)
                        discount = MaximumDiscountAmount.Value;
                    break;

                case DiscountType.FixedAmount:
                    discount = DiscountValue;
                    break;

                case DiscountType.FixedPrice:
                    discount = originalPrice - DiscountValue;
                    if (discount < 0) discount = 0;
                    break;
            }

            return discount * quantity;
        }

        public bool IsEligibleForCustomer(ApplicationUser? user, decimal cartTotal, int customerOrderCount = 0)
        {
            if (!IsCurrentlyActive)
                return false;

            // Check customer type restrictions
            if (ForNewCustomersOnly && customerOrderCount > 0)
                return false;

            if (ForExistingCustomersOnly && customerOrderCount == 0)
                return false;

            // Check customer minimum requirements
            if (CustomerMinOrderCount.HasValue && customerOrderCount < CustomerMinOrderCount.Value)
                return false;

            if (CustomerMinPurchaseAmount.HasValue && cartTotal < CustomerMinPurchaseAmount.Value)
                return false;

            return true;
        }

        public bool IsApplicableToProduct(Product product)
        {
            if (!IsCurrentlyActive)
                return false;

            if (ApplyToAllProducts)
                return true;

            if (ProductId.HasValue && product.Id == ProductId.Value)
                return true;

            if (CategoryId.HasValue && product.CategoryId == CategoryId.Value)
                return true;

            if (BrandId.HasValue && product.BrandId == BrandId.Value)
                return true;

            return false;
        }

        public void IncrementUsage()
        {
            UsedCount++;
            UpdatedAt = DateTime.Now;
        }

        public bool CanBeUsedByCustomer(string userId, ICollection<Order> customerOrders)
        {
            if (!IsCurrentlyActive)
                return false;

            if (UsageLimitPerCustomer.HasValue)
            {
                var customerUsageCount = customerOrders
                    .Count(o => o.PromotionCode == Code && o.ApplicationUserId == userId);

                return customerUsageCount < UsageLimitPerCustomer.Value;
            }

            return true;
        }
    }

    public enum DiscountType
    {
        Percentage = 1,
        FixedAmount = 2,
        FixedPrice = 3,
        FreeShipping = 4,
        BuyXGetY = 5
    }
}
