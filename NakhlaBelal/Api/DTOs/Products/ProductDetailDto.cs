namespace NakhlaBelal.Api.DTOs.Products;

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string MainImage { get; set; } = "";
    public List<string> GalleryImages { get; set; } = new();
    public decimal Price { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsOnSale { get; set; }
    public decimal? Discount { get; set; }
    public decimal? SpecialPrice { get; set; }
    public string CategoryName { get; set; } = "";
    public int? CategoryId { get; set; }
    public string? BrandName { get; set; }
    public string? ColorName { get; set; }
    public string? FabricTypeName { get; set; }
    public bool IsNew { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsOutOfStock { get; set; }
    public bool IsLowStock { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = "";
    public int MinimumQty { get; set; }
    public int QtyStep { get; set; }
    public string? Pattern { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int ViewsCount { get; set; }
    public List<ProductCompositionDetailDto> Compositions { get; set; } = new();
    public List<ProductReviewDto> Reviews { get; set; } = new();
}

public class ProductCompositionDetailDto
{
    public string CompositionName { get; set; } = "";
    public decimal Percentage { get; set; }
}

public class ProductReviewDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
    public DateTime CreatedAt { get; set; }
}
