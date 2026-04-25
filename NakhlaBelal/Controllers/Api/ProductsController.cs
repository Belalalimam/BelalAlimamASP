using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.Api.DTOs.Products;
using NakhlaBelal.Api.Wrappers;
using NakhlaBelal.DataAccess;
using NakhlaBelal.ViewModels;

namespace NakhlaBelal.Controllers.Api;

public class ProductsController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] FilterVM filter, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Color)
            .Include(p => p.FabricType)
            .Where(p => p.Status)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(p => p.Name.Contains(filter.Name));
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);
        if (filter.FabricTypeId.HasValue)
            query = query.Where(p => p.FabricTypeId == filter.FabricTypeId);
        if (filter.ColorId.HasValue)
            query = query.Where(p => p.ColorId == filter.ColorId);
        if (filter.CompositionId.HasValue)
            query = query.Where(p => p.ProductCompositions.Any(pc => pc.CompositionId == filter.CompositionId));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(p => new ProductListDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug ?? "",
            MainImage = p.MainImage ?? "",
            Price = p.Price,
            FinalPrice = p.FinalPrice,
            IsOnSale = p.IsOnSale,
            DiscountPercent = p.IsOnSale ? p.Discount : null,
            CategoryName = p.Category?.Name ?? "",
            BrandName = p.Brand?.Name,
            IsNew = p.IsNew,
            IsFeatured = p.IsFeatured,
            IsBestSeller = p.IsBestSeller,
            IsOutOfStock = p.IsOutOfStock,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            Unit = p.Unit.ToString()
        }).ToList();

        return Ok(new PagedResponse<ProductListDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Color)
            .Include(p => p.FabricType)
            .Include(p => p.ProductCompositions).ThenInclude(pc => pc.Composition)
            .Include(p => p.Reviews.Where(r => r.IsApproved))
            .FirstOrDefaultAsync(p => p.Id == id && p.Status);

        if (product == null) return ApiFail("Product not found", 404);

        return ApiOk(MapToDetail(product));
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Color)
            .Include(p => p.FabricType)
            .Include(p => p.ProductCompositions).ThenInclude(pc => pc.Composition)
            .Include(p => p.Reviews.Where(r => r.IsApproved))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status);

        if (product == null) return ApiFail("Product not found", 404);

        return ApiOk(MapToDetail(product));
    }

    private static ProductDetailDto MapToDetail(Models.Product p)
    {
        return new ProductDetailDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug ?? "",
            Description = p.Description,
            MainImage = p.MainImage ?? "",
            GalleryImages = p.GalleryImages,
            Price = p.Price,
            FinalPrice = p.FinalPrice,
            IsOnSale = p.IsOnSale,
            Discount = p.Discount,
            SpecialPrice = p.SpecialPrice,
            CategoryName = p.Category?.Name ?? "",
            CategoryId = p.CategoryId,
            BrandName = p.Brand?.Name,
            ColorName = p.Color?.Name,
            FabricTypeName = p.FabricType?.Name,
            IsNew = p.IsNew,
            IsFeatured = p.IsFeatured,
            IsBestSeller = p.IsBestSeller,
            IsOutOfStock = p.IsOutOfStock,
            IsLowStock = p.IsLowStock,
            StockQuantity = p.StockQuantity,
            Unit = p.Unit.ToString(),
            MinimumQty = p.MinimumQuantity,
            QtyStep = p.QuantityStep,
            Pattern = p.Pattern.ToString(),
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            ViewsCount = p.ViewsCount,
            Compositions = p.ProductCompositions?.Select(pc => new ProductCompositionDetailDto
            {
                CompositionName = pc.Composition?.Name ?? "",
                Percentage = pc.Percentage
            }).ToList() ?? new(),
            Reviews = p.Reviews?.Select(r => new ProductReviewDto
            {
                Id = r.Id,
                CustomerName = r.CustomerName ?? "",
                Rating = r.Rating,
                ReviewText = r.ReviewText,
                CreatedAt = r.CreatedAt
            }).ToList() ?? new()
        };
    }
}
