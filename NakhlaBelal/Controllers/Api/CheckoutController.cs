using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.Api.DTOs.Checkout;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using NakhlaBelal.Utitlies;
using Stripe.Checkout;

namespace NakhlaBelal.Controllers.Api;

[Route("api/checkout")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CheckoutController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IAdminEmailNotifier _adminEmailNotifier;
    private readonly ILogger<CheckoutController> _logger;
    private readonly IConfiguration _config;

    public CheckoutController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWhatsAppService whatsAppService,
        IAdminEmailNotifier adminEmailNotifier,
        ILogger<CheckoutController> logger,
        IConfiguration config)
    {
        _context = context;
        _userManager = userManager;
        _whatsAppService = whatsAppService;
        _adminEmailNotifier = adminEmailNotifier;
        _logger = logger;
        _config = config;
    }

    [HttpPost("place-order")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        if (!ModelState.IsValid) return ApiErrors("Validation failed");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var cartItems = await _context.Carts
            .Include(c => c.Product)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
            return ApiFail("Your cart is empty");

        decimal subtotal = cartItems.Sum(c => c.Price * c.Count);
        decimal discount = 0;

        if (!string.IsNullOrEmpty(request.PromotionCode))
        {
            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Code == request.PromotionCode && p.IsActive && p.IsValid
                                         && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);
            if (promo != null)
                discount = cartItems.Sum(c => promo.CalculateDiscount(c.Price, c.Count));
        }

        decimal tax = subtotal * 0.08m;
        decimal total = subtotal - discount + tax + request.ShippingCost;

        var order = new Order
        {
            ApplicationUserId = userId,
            OrderNumber = Order.GenerateOrderNumber(),
            ShippingFirstName = request.ShippingFirstName,
            ShippingLastName = request.ShippingLastName,
            ShippingAddress = request.ShippingAddress,
            ShippingCity = request.ShippingCity,
            ShippingState = request.ShippingState,
            ShippingZipCode = request.ShippingZipCode,
            ShippingCountry = request.ShippingCountry,
            ShippingPhone = request.ShippingPhone,
            ShippingEmail = request.ShippingEmail,
            BillingFirstName = request.BillingSameAsShipping ? request.ShippingFirstName : request.BillingFirstName,
            BillingLastName = request.BillingSameAsShipping ? request.ShippingLastName : request.BillingLastName,
            BillingAddress = request.BillingSameAsShipping ? request.ShippingAddress : request.BillingAddress,
            BillingCity = request.BillingSameAsShipping ? request.ShippingCity : request.BillingCity,
            BillingState = request.BillingSameAsShipping ? request.ShippingState : request.BillingState,
            BillingZipCode = request.BillingSameAsShipping ? request.ShippingZipCode : request.BillingZipCode,
            BillingCountry = request.BillingSameAsShipping ? request.ShippingCountry : request.BillingCountry,
            Subtotal = subtotal,
            ShippingCost = request.ShippingCost,
            TaxAmount = tax,
            DiscountAmount = discount,
            TotalAmount = total,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "Pending",
            OrderStatus = "Pending",
            PromotionCode = request.PromotionCode,
            CustomerNotes = request.CustomerNotes,
            CreatedAt = DateTime.Now
        };

        string? stripeUrl = null;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            order.OrderItems = new List<OrderItem>();
            foreach (var c in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Count,
                    UnitPrice = c.Price,
                    TotalPrice = c.Price * c.Count,
                    ProductName = c.Product?.Name ?? "",
                    ProductImage = c.Product?.MainImage ?? "",
                    ProductSku = c.Product?.SKU ?? ""
                });
            }

            await _context.Orders.AddAsync(order);
            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to place order for user {UserId}", userId);
            return ApiFail("Failed to place order. Please try again.", 500);
        }

        // Send notifications (non-blocking)
        _ = Task.Run(async () =>
        {
            try { await _adminEmailNotifier.NotifyNewOrderAsync(order); } catch { }
            try { await _whatsAppService.SendOrderConfirmationAsync(order, ""); } catch { }
        });

        // Handle Stripe payment
        if (request.PaymentMethod == "Stripe")
        {
            try
            {
                var stripeKey = _config["Stripe:SecretKey"] ?? "";
                Stripe.StripeConfiguration.ApiKey = stripeKey;

                var sessionOptions = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = order.OrderItems.Select(item => new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "egp",
                            UnitAmount = (long)(item.UnitPrice * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.ProductName ?? "Product"
                            }
                        },
                        Quantity = item.Quantity
                    }).ToList(),
                    Mode = "payment",
                    SuccessUrl = $"{_config["WhatsApp:SiteBaseUrl"]}/Customer/Checkout/Success?session_id={{CHECKOUT_SESSION_ID}}&orderId={order.Id}",
                    CancelUrl = $"{_config["WhatsApp:SiteBaseUrl"]}/Customer/Checkout/Cancel",
                    CustomerEmail = order.ShippingEmail,
                    Metadata = new Dictionary<string, string> { { "orderId", order.Id.ToString() } }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(sessionOptions);
                order.PaymentTransactionId = session.Id;
                await _context.SaveChangesAsync();
                stripeUrl = session.Url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe session creation failed for order {OrderId}", order.Id);
            }
        }

        return ApiOk(new OrderResultDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            PaymentMethod = order.PaymentMethod,
            StripeCheckoutUrl = stripeUrl
        }, "Order placed successfully");
    }
}
