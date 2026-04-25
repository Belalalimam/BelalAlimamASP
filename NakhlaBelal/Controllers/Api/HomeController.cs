using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.Api.DTOs.Products;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Controllers.Api;

[Route("api/home")]
public class HomeController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context) => _context = context;

    [HttpGet("featured")]
    public async Task<IActionResult> Featured(int limit = 12)
    {
        var products = await GetProductList(q => q.Where(p => p.IsFeatured), limit);
        return ApiOk(products);
    }

    [HttpGet("new-arrivals")]
    public async Task<IActionResult> NewArrivals(int limit = 12)
    {
        var products = await GetProductList(q => q.Where(p => p.IsNew), limit);
        return ApiOk(products);
    }

    [HttpGet("on-sale")]
    public async Task<IActionResult> OnSale(int limit = 12)
    {
        var products = await GetProductList(q => q.Where(p => p.IsOnSale), limit);
        return ApiOk(products);
    }

    [HttpGet("best-sellers")]
    public async Task<IActionResult> BestSellers(int limit = 12)
    {
        var products = await GetProductList(q => q.Where(p => p.IsBestSeller), limit);
        return ApiOk(products);
    }

    private async Task<List<ProductListDto>> GetProductList(
        Func<IQueryable<Models.Product>, IQueryable<Models.Product>> filter, int limit)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.Status);

        var items = await filter(query)
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return items.Select(p => new ProductListDto
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
    }
}
