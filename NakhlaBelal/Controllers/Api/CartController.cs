using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.Api.DTOs.Cart;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using NakhlaBelal.Repositories.IRepositories;

namespace NakhlaBelal.Controllers.Api;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CartController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository<Cart> _cartRepo;
    private readonly IRepository<Promotion> _promoRepo;

    public CartController(
        ApplicationDbContext context,
        IRepository<Cart> cartRepo,
        IRepository<Promotion> promoRepo)
    {
        _context = context;
        _cartRepo = cartRepo;
        _promoRepo = promoRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var items = await _context.Carts
            .Include(c => c.Product)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        var subtotal = items.Sum(c => c.Price * c.Count);
        var dto = new CartDto
        {
            Items = items.Select(c => new CartItemDto
            {
                ProductId = c.ProductId,
                ProductName = c.Product?.Name ?? "",
                ProductImage = c.Product?.MainImage ?? "",
                ProductSlug = c.Product?.Slug ?? "",
                UnitPrice = c.Price,
                Quantity = c.Count,
                LineTotal = c.Price * c.Count,
                Unit = c.Product?.Unit.ToString() ?? "",
                IsOutOfStock = c.Product?.IsOutOfStock ?? false
            }).ToList(),
            Subtotal = subtotal,
            Total = subtotal,
            ItemCount = items.Sum(c => c.Count)
        };

        return ApiOk(dto);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (!ModelState.IsValid) return ApiErrors("Validation failed");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var product = await _context.Products.FindAsync(request.ProductId);

        if (product == null) return ApiFail("Product not found", 404);
        if (product.IsOutOfStock) return ApiFail("Product is out of stock");

        var existing = await _context.Carts
            .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductId == request.ProductId);

        if (existing != null)
        {
            existing.Count += request.Quantity;
            existing.UpdatedAt = DateTime.Now;
        }
        else
        {
            await _cartRepo.AddAsync(new Cart
            {
                ApplicationUserId = userId,
                ProductId = request.ProductId,
                Count = request.Quantity,
                Price = product.FinalPrice
            });
        }

        await _cartRepo.CommitAsync();
        return ApiOk<object>(null!, "Added to cart");
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request)
    {
        if (!ModelState.IsValid) return ApiErrors("Validation failed");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await _context.Carts
            .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductId == request.ProductId);

        if (item == null) return ApiFail("Cart item not found", 404);

        item.Count = request.Quantity;
        item.UpdatedAt = DateTime.Now;
        await _cartRepo.CommitAsync();

        return ApiOk<object>(null!, "Cart updated");
    }

    [HttpDelete("item/{productId:int}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await _context.Carts
            .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);

        if (item == null) return ApiFail("Cart item not found", 404);

        _cartRepo.Delete(item);
        await _cartRepo.CommitAsync();

        return ApiOk<object>(null!, "Item removed");
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await _context.Carts
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        foreach (var item in items)
            _cartRepo.Delete(item);

        await _cartRepo.CommitAsync();
        return ApiOk<object>(null!, "Cart cleared");
    }

    [HttpPost("apply-promo")]
    public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest request)
    {
        var promo = await _context.Promotions
            .FirstOrDefaultAsync(p => p.Code == request.Code && p.IsActive &&
                                      p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);

        if (promo == null)
            return ApiFail("Invalid or expired promotion code");

        return ApiOk(new { promo.Code, promo.DiscountType, promo.DiscountValue }, "Promotion applied");
    }
}
