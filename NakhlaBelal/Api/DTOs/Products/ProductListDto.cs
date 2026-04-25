namespace NakhlaBelal.Api.DTOs.Products;

public class ProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string MainImage { get; set; } = "";
    public decimal Price { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsOnSale { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string CategoryName { get; set; } = "";
    public string? BrandName { get; set; }
    public bool IsNew { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsOutOfStock { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string Unit { get; set; } = "";
}
