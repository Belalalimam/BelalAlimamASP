// Areas/Customer/Controllers/CheckoutController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Models;
using Stripe;
using Stripe.Checkout;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace NAKHLA.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IRepository<Promotion> promotionRepository,
            ILogger<CheckoutController> logger)
        {
            _userManager = userManager;
            _context = context;
            _promotionRepository = promotionRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["error-notification"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }
            //2.جلب الخصم من الـ Session(نفس منطق الـ Index تماماً)
            var appliedCode = HttpContext.Session.GetString("AppliedPromotionCode");
            Promotion? promotion = null;
            if (!string.IsNullOrEmpty(appliedCode))
            {
                promotion = await _promotionRepository.GetOneAsync(e => e.Code.ToLower() == appliedCode.ToLower() && e.IsActive && e.IsValid);
            }

            decimal subtotalOriginal = 0;
            decimal totalDiscount = 0;
            decimal taxTotal = 0;
            decimal taxRate = 0.19m;

            //    // 3. الحسبة الثلاثية (سعر أصلي + خصم + ضريبة)
            foreach (var item in cartItems)
            {
                subtotalOriginal += item.Product.Price * item.Count;

                // حساب الخصم إذا وجد
                if (promotion != null && promotion.IsCurrentlyActive && promotion.IsApplicableToProduct(item.Product))
                {
                    decimal discountPerUnit = promotion.CalculateDiscount(item.Product.Price, 1);
                    item.Price = item.Product.Price - discountPerUnit;
                    totalDiscount += (discountPerUnit * item.Count);
                }
                else { item.Price = item.Product.Price; }

                // حساب الضريبة على السعر بعد الخصم (أو قبل حسب قانون بلدك، غالباً بعد الخصم)
                if (item.Product.Taxable)
                {
                    taxTotal += (item.Price * item.Count) * taxRate;
                }
            }

            var checkoutVM = new CheckoutVM
            {
                CartData = new CartVM
                {
                    CartItems = cartItems,
                    Subtotal = subtotalOriginal,
                    Discount = totalDiscount,
                    Tax = taxTotal,
                    Shipping = 5.99m,
                    Total = (subtotalOriginal - totalDiscount) + taxTotal + 5.99m,
                    PromotionCode = appliedCode
                },
                // بيانات العميل تلقائياً
                ShippingFirstName = user?.FirstName,
                ShippingLastName = user?.LastName,
                ShippingEmail = user?.Email,
                ShippingAddress = user?.Address,
                ShippingCity = user?.City,
                ShippingPhone = user?.PhoneNumber,
                ShippingZipCode = user?.ZipCode,
                ShippingState = user?.State,
                ShippingCountry = user?.Country
            };

            return View(checkoutVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(CheckoutVM model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                var cartItems = await _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.ApplicationUserId == userId)
                    .ToListAsync();

                model.CartItems = cartItems;
                return View("Index", model);
            }

            try
            {
                // Get cart items
                var cartItems = await _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.ApplicationUserId == userId)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["error-notification"] = "Your cart is empty";
                    return RedirectToAction("Index", "Cart");
                }

                // Create order
                var order = new Order
                {
                    ApplicationUserId = userId, // THIS IS CRITICAL
                    OrderNumber = Order.GenerateOrderNumber(),

                    // Shipping info
                    ShippingFirstName = model.ShippingFirstName,
                    ShippingLastName = model.ShippingLastName,
                    ShippingAddress = model.ShippingAddress,
                    ShippingCity = model.ShippingCity,
                    ShippingState = model.ShippingState,
                    ShippingZipCode = model.ShippingZipCode,
                    ShippingCountry = model.ShippingCountry,
                    ShippingPhone = model.ShippingPhone,
                    ShippingEmail = model.ShippingEmail,

                    // Billing info
                    BillingFirstName = model.BillingSameAsShipping ? model.ShippingFirstName : model.BillingFirstName,
                    BillingLastName = model.BillingSameAsShipping ? model.ShippingLastName : model.BillingLastName,
                    BillingAddress = model.BillingSameAsShipping ? model.ShippingAddress : model.BillingAddress,
                    BillingCity = model.BillingSameAsShipping ? model.ShippingCity : model.BillingCity,
                    BillingState = model.BillingSameAsShipping ? model.ShippingState : model.BillingState,
                    BillingZipCode = model.BillingSameAsShipping ? model.ShippingZipCode : model.BillingZipCode,
                    BillingCountry = model.BillingSameAsShipping ? model.ShippingCountry : model.BillingCountry,

                    // Order amounts
                    Subtotal = cartItems.Sum(c => c.Price * c.Count),
                    ShippingCost = model.ShippingCost,
                    TaxAmount = cartItems.Sum(c => c.Price * c.Count) * 0.08m,
                    DiscountAmount = model.DiscountAmount,
                    TotalAmount = cartItems.Sum(c => c.Price * c.Count) + model.ShippingCost + (cartItems.Sum(c => c.Price * c.Count) * 0.08m) - model.DiscountAmount,

                    // Payment info
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = "Pending",

                    // Order status
                    OrderStatus = "Pending",

                    // Customer notes
                    CustomerNotes = model.CustomerNotes,

                    CreatedAt = DateTime.Now
                };

                // Add order items
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Count,
                        UnitPrice = cartItem.Price,
                        TotalPrice = cartItem.Price * cartItem.Count,
                        ProductName = cartItem.Product?.Name ?? "Unknown Product",
                        ProductImage = cartItem.Product?.MainImage,
                        ProductSku = cartItem.Product?.SKU
                    };

                    order.OrderItems.Add(orderItem);
                }

                // Save order
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                // Clear cart
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                // Redirect to payment or confirmation
                if (model.PaymentMethod == "Credit Card")
                {
                    return RedirectToAction("ProcessPayment", new { orderId = order.Id });
                }
                else
                {
                    // For cash on delivery or other methods
                    TempData["success-notification"] = $"Order #{order.OrderNumber} placed successfully!";
                    return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order");
                TempData["error-notification"] = "An error occurred while processing your order";
                return View("Index", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProcessPayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Create Stripe session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/Customer/Checkout/Success?session_id={{CHECKOUT_SESSION_ID}}&orderId={orderId}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/Customer/Checkout/Cancel?orderId={orderId}",
                CustomerEmail = order.ShippingEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", orderId.ToString() },
                    { "orderNumber", order.OrderNumber }
                }
            };

            foreach (var item in order.OrderItems)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        UnitAmount = (long)(item.UnitPrice * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.ProductName,
                            Images = !string.IsNullOrEmpty(item.ProductImage) ?
                                new List<string> { item.ProductImage } : null
                        }
                    },
                    Quantity = item.Quantity,
                });
            }

            // Add shipping if any
            if (order.ShippingCost > 0)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        UnitAmount = (long)(order.ShippingCost * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Shipping Fee"
                        }
                    },
                    Quantity = 1,
                });
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Update order with Stripe session ID
            order.PaymentTransactionId = session.Id;
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }

        [HttpGet]
        public async Task<IActionResult> Success(string session_id, int orderId)
        {
            var service = new SessionService();
            var session = await service.GetAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.PaymentStatus = "Paid";
                    order.PaymentDate = DateTime.Now;
                    order.OrderStatus = "Processing";
                    order.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["success-notification"] = $"Payment successful! Order #{order.OrderNumber} is being processed.";
                    return View(order);
                }
            }

            TempData["error-notification"] = "Payment verification failed";
            return RedirectToAction("Index", "Cart");
        }

        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }

}