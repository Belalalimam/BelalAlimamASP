using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.Api.DTOs.Wishlist;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Controllers.Api;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WishlistController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public WishlistController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var items = await _context.Wishlists
            .Include(w => w.Product)
            .Where(w => w.ApplicationUserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WishlistItemDto
            {
                Id = w.Id,
                ProductId = w.ProductId,
                ProductName = w.Product != null ? w.Product.Name : "",
                ProductImage = w.Product != null ? (w.Product.MainImage ?? "") : "",
                ProductSlug = w.Product != null ? (w.Product.Slug ?? "") : "",
                FinalPrice = w.Product != null ? w.Product.FinalPrice : 0,
                IsOutOfStock = w.Product != null && w.Product.IsOutOfStock
            })
            .ToListAsync();

        return ApiOk(items);
    }

    [HttpPost("toggle/{productId:int}")]
    public async Task<IActionResult> Toggle(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var existing = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == productId);

        if (existing != null)
        {
            _context.Wishlists.Remove(existing);
            await _context.SaveChangesAsync();
            return ApiOk<object>(null!, "Removed from wishlist");
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null) return ApiFail("Product not found", 404);

        _context.Wishlists.Add(new Wishlist
        {
            ApplicationUserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        return ApiOk<object>(null!, "Added to wishlist");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.Id == id && w.ApplicationUserId == userId);

        if (item == null) return ApiFail("Wishlist item not found", 404);

        _context.Wishlists.Remove(item);
        await _context.SaveChangesAsync();

        return ApiOk<object>(null!, "Removed from wishlist");
    }
}
