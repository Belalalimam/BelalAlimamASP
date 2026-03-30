using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NakhlaBelal.Models;
using Stripe.Checkout;
using System.Security.Claims;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Promotion> _promotionRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CartController> _logger;

        public CartController(
            UserManager<ApplicationUser> userManager,
            IRepository<Cart> cartRepository,
            IRepository<Promotion> promotionRepository,
            IProductRepository productRepository,
            ILogger<CartController> logger)
        {
            _userManager = userManager;
            _cartRepository = cartRepository;
            _promotionRepository = promotionRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        // GET: /Customer/Cart        
        [HttpGet]
        public async Task<IActionResult> Index(string? code = null, bool removePromo = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = (await _cartRepository.GetAsync(e => e.ApplicationUserId == userId, includes: [e => e.Product])).ToList();

            // حذف الكوبون وإعادة الأسعار الأصلية
            if (removePromo)
            {
                HttpContext.Session.Remove("AppliedPromotionCode");
                foreach (var item in cartItems)
                {
                    item.Price = item.Product.Price;
                    await _cartRepository.UpdateAsync(item);
                }
                await _cartRepository.CommitAsync();
                return RedirectToAction(nameof(Index));
            }

            // حفظ الكود الجديد في السيشن وعمل Refresh
            if (!string.IsNullOrEmpty(code))
            {
                HttpContext.Session.SetString("AppliedPromotionCode", code);
                return RedirectToAction(nameof(Index));
            }

            var appliedCode = HttpContext.Session.GetString("AppliedPromotionCode");
            Promotion? promotion = null;
            if (!string.IsNullOrEmpty(appliedCode))
            {
                // تم الحل: استدعاء الحقول الأساسية فقط لتجنب الخطأ الذي ظهر بالصورة
                promotion = await _promotionRepository.GetOneAsync(e => e.Code.ToLower() == appliedCode.ToLower() && e.IsActive && e.IsValid);
            }

            decimal subtotalOriginal = 0;
            decimal totalDiscount = 0;

            foreach (var item in cartItems)
            {
                subtotalOriginal += item.Product.Price * item.Count;

                // التحقق من الصلاحية يتم هنا في الذاكرة (C#)
                if (promotion != null && promotion.IsCurrentlyActive && promotion.IsApplicableToProduct(item.Product))
                {
                    decimal discountPerUnit = promotion.CalculateDiscount(item.Product.Price, 1);
                    item.Price = item.Product.Price - discountPerUnit;
                    totalDiscount += (discountPerUnit * item.Count);
                }
                else
                {
                    item.Price = item.Product.Price;
                }
                await _cartRepository.UpdateAsync(item);
            }
            await _cartRepository.CommitAsync();

            // تعبئة الموديل بالقيم التي ينتظرها الـ View الخاص بك
            var vm = new CartVM
            {
                CartItems = cartItems,
                Subtotal = subtotalOriginal,      // سيظهر في خانة SUBTOTAL
                Discount = totalDiscount,          // سيظهر في خانة DISCOUNT
                Shipping = 5.99m,
                Total = (subtotalOriginal - totalDiscount) + 5.99m, // سيظهر في خانة TOTAL
                PromotionCode = appliedCode,
                PromotionName = promotion?.Name
            };

            return View(vm);
        }


        // POST: /Customer/Cart/AddToCart
        [HttpPost]
        //[ValidateAntiForgeryToken]
        [Route("Customer/Cart/AddToCart")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int count = 1, CancellationToken cancellationToken = default)
        {
            try
            {
              
                if (count <= 0)
                {
                    TempData["error-notification"] = "Quantity must be at least 1";
                    return RedirectToAction("Details", "Product", new { id = productId, area = "" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["error-notification"] = "Please login to add items to cart";
                    return RedirectToAction("Login", "Account", new { area = "Identity", returnUrl = Url.Action("Details", "Product", new { id = productId, area = "" }) });
                }

                var product = await _productRepository.GetOneAsync(e => e.Id == productId);
                if (product == null)
                {
                    TempData["error-notification"] = "Product not found";
                    return RedirectToAction("Index", "Home");
                }

                if (!product.Status || product.IsDeleted)
                {
                    TempData["error-notification"] = "Product is not available";
                    return RedirectToAction("Details", "Product", new { id = productId, area = "" });
                }

                if (product.ManageStock && product.StockQuantity < count)
                {
                    TempData["error-notification"] = $"Only {product.StockQuantity} items available in stock";
                    return RedirectToAction("Details", "Product", new { id = productId, area = "" });
                }

                var existingCartItem = await _cartRepository.GetOneAsync(
                    e => e.ApplicationUserId == userId && e.ProductId == productId);

                if (existingCartItem != null)
                {
                    // Update existing cart item
                    var newCount = existingCartItem.Count + count;

                    if (product.ManageStock && product.StockQuantity < newCount)
                    {
                        TempData["error-notification"] = $"Cannot add {count} more items. Only {product.StockQuantity - existingCartItem.Count} available in stock";
                        return RedirectToAction("Details", "Product", new { id = productId, area = "" });
                    }

                    existingCartItem.Count = newCount;
                    existingCartItem.UpdatedAt = DateTime.Now;

                    await _cartRepository.UpdateAsync(existingCartItem);
                    await _cartRepository.CommitAsync(cancellationToken);

                    TempData["success-notification"] = "Product quantity updated in cart";
                }
                else
                {
                    // Add new cart item
                    var cartItem = new Cart
                    {
                        ProductId = productId,
                        ApplicationUserId = userId,
                        Count = count,
                        Price = product.FinalPrice, // Use the final price (with discounts)
                        CreatedAt = DateTime.Now
                    };

                    await _cartRepository.AddAsync(cartItem, cancellationToken: cancellationToken);
                    await _cartRepository.CommitAsync(cancellationToken);

                    TempData["success-notification"] = "Product added to cart successfully";
                }

                return RedirectToAction("Index", "Cart", new { area = "Customer" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                TempData["error-notification"] = "An error occurred while adding product to cart";
                return RedirectToAction("Details", "Product", new { id = productId, area = "" });
            }
        }

        

        // POST: /Customer/Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity, CancellationToken cancellationToken = default)
        {
            try
            {
                if (quantity <= 0)
                {
                    return await DeleteProduct(productId, cancellationToken);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var cartItem = await _cartRepository.GetOneAsync(
                    e => e.ApplicationUserId == userId && e.ProductId == productId,
                    includes: [e => e.Product]);

                if (cartItem == null)
                {
                    return NotFound();
                }

                // Check stock availability
                if (cartItem.Product.ManageStock && cartItem.Product.StockQuantity < quantity)
                {
                    TempData["error-notification"] = $"Only {cartItem.Product.StockQuantity} items available in stock";
                    return RedirectToAction("Index");
                }

                cartItem.Count = quantity;
                cartItem.UpdatedAt = DateTime.Now;

                await _cartRepository.UpdateAsync(cartItem);
                await _cartRepository.CommitAsync(cancellationToken);

                TempData["success-notification"] = "Quantity updated successfully";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                TempData["error-notification"] = "An error occurred while updating quantity";
                return RedirectToAction("Index");
            }
        }

        // GET: /Customer/Cart/IncrementProduct
        [HttpGet]
        public async Task<IActionResult> IncrementProduct(int productId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var cartItem = await _cartRepository.GetOneAsync(
                    e => e.ApplicationUserId == userId && e.ProductId == productId,
                    includes: [e => e.Product]);

                if (cartItem == null)
                {
                    TempData["error-notification"] = "Product not found in cart";
                    return RedirectToAction("Index");
                }

                // Check stock availability
                if (cartItem.Product.ManageStock && cartItem.Product.StockQuantity <= cartItem.Count)
                {
                    TempData["error-notification"] = "No more items available in stock";
                    return RedirectToAction("Index");
                }

                cartItem.Count++;
                cartItem.UpdatedAt = DateTime.Now;

                await _cartRepository.UpdateAsync(cartItem);
                await _cartRepository.CommitAsync(cancellationToken);

                TempData["success-notification"] = "Quantity increased";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing product quantity");
                TempData["error-notification"] = "An error occurred";
                return RedirectToAction("Index");
            }
        }

        // GET: /Customer/Cart/DecrementProduct
        [HttpGet]
        public async Task<IActionResult> DecrementProduct(int productId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var cartItem = await _cartRepository.GetOneAsync(
                    e => e.ApplicationUserId == userId && e.ProductId == productId);

                if (cartItem == null)
                {
                    TempData["error-notification"] = "Product not found in cart";
                    return RedirectToAction("Index");
                }

                if (cartItem.Count <= 1)
                {
                    return await DeleteProduct(productId, cancellationToken);
                }

                cartItem.Count--;
                cartItem.UpdatedAt = DateTime.Now;

                await _cartRepository.UpdateAsync(cartItem);
                await _cartRepository.CommitAsync(cancellationToken);

                TempData["success-notification"] = "Quantity decreased";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrementing product quantity");
                TempData["error-notification"] = "An error occurred";
                return RedirectToAction("Index");
            }
        }

        // GET: /Customer/Cart/DeleteProduct
        [HttpGet]
        public async Task<IActionResult> DeleteProduct(int productId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var cartItem = await _cartRepository.GetOneAsync(
                    e => e.ApplicationUserId == userId && e.ProductId == productId);

                if (cartItem == null)
                {
                    TempData["error-notification"] = "Product not found in cart";
                    return RedirectToAction("Index");
                }

                _cartRepository.Delete(cartItem);
                await _cartRepository.CommitAsync(cancellationToken);

                TempData["success-notification"] = "Product removed from cart";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product from cart");
                TempData["error-notification"] = "An error occurred while removing product";
                return RedirectToAction("Index");
            }
        }

        // GET: /Customer/Cart/ClearCart
        [HttpGet]
        public async Task<IActionResult> ClearCart(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var cartItems = await _cartRepository.GetAsync(e => e.ApplicationUserId == userId);

                foreach (var item in cartItems)
                {
                    _cartRepository.Delete(item);
                }

                await _cartRepository.CommitAsync(cancellationToken);

                TempData["success-notification"] = "Cart cleared successfully";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["error-notification"] = "An error occurred while clearing cart";
                return RedirectToAction("Index");
            }
        }

        // GET: /Customer/Cart/GetCartCount
        [HttpGet]
        public async Task<JsonResult> GetCartCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { count = 0 });
                }

                var count = await _cartRepository.CountAsync(e => e.ApplicationUserId == userId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }

        // GET: /Customer/Cart/GetCartTotal
        [HttpGet]
        public async Task<JsonResult> GetCartTotal()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { total = 0 });
                }

                var cartItems = await _cartRepository.GetAsync(
                    e => e.ApplicationUserId == userId,
                    includes: [e => e.Product]);

                var total = cartItems.Sum(e => e.Price * e.Count);
                return Json(new { total = total.ToString("N2") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart total");
                return Json(new { total = "0.00" });
            }
        }

        // GET: /Customer/Cart/Pay
        [HttpGet]
        public async Task<IActionResult> Pay()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["error-notification"] = "Please login to proceed to checkout";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var cartItems = await _cartRepository.GetAsync(
                    e => e.ApplicationUserId == userId,
                    includes: [e => e.Product]);

                if (!cartItems.Any())
                {
                    TempData["error-notification"] = "Your cart is empty";
                    return RedirectToAction("Index");
                }

                // Validate stock availability
                var outOfStockItems = cartItems
                    .Where(e => e.Product.ManageStock && e.Product.StockQuantity < e.Count)
                    .ToList();

                if (outOfStockItems.Any())
                {
                    var productNames = string.Join(", ", outOfStockItems.Select(e => e.Product.Name));
                    TempData["error-notification"] = $"The following items have insufficient stock: {productNames}";
                    return RedirectToAction("Index");
                }

                // Create Stripe checkout session
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = $"{Request.Scheme}://{Request.Host}/Customer/Cart/Success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{Request.Scheme}://{Request.Host}/Customer/Cart",
                    CustomerEmail = User.FindFirstValue(ClaimTypes.Email),
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId }
                    }
                };

                foreach (var item in cartItems)
                {
                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd", // Change to your currency
                            UnitAmount = (long)(item.Price * 100), // Convert to cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Name,
                                Description = item.Product.ShortDescription,
                                Images = !string.IsNullOrEmpty(item.Product.MainImage) ?
                                    new List<string> { item.Product.MainImage } : null
                            }
                        },
                        Quantity = item.Count,
                    });
                }

                // Add shipping fee if applicable
                var shipping = 0m; // Calculate shipping here
                if (shipping > 0)
                {
                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(shipping * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Shipping Fee",
                                Description = "Standard shipping"
                            }
                        },
                        Quantity = 1,
                    });
                }

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                // Store session ID temporarily
                HttpContext.Session.SetString("StripeSessionId", session.Id);

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment session");
                TempData["error-notification"] = "An error occurred while processing payment";
                return RedirectToAction("Index");
            }
        }

        // GET: /Customer/Cart/Success
        [HttpGet]
        public async Task<IActionResult> Success(string session_id)
        {
            try
            {
                if (string.IsNullOrEmpty(session_id))
                {
                    TempData["error-notification"] = "Invalid payment session";
                    return RedirectToAction("Index");
                }

                var service = new SessionService();
                var session = await service.GetAsync(session_id);

                if (session.PaymentStatus != "paid")
                {
                    TempData["error-notification"] = "Payment not completed";
                    return RedirectToAction("Index");
                }

                // Get user ID from session metadata
                var userId = session.Metadata["userId"];

                // Clear cart after successful payment
                var cartItems = await _cartRepository.GetAsync(e => e.ApplicationUserId == userId);
                foreach (var item in cartItems)
                {
                    _cartRepository.Delete(item);
                }
                await _cartRepository.CommitAsync();

                // Here you should create an order record in your database
                // TODO: Create order logic

                TempData["success-notification"] = "Payment successful! Thank you for your order.";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing successful payment");
                TempData["error-notification"] = "An error occurred while processing your order";
                return RedirectToAction("Index");
            }
        }



        // GET: /Customer/Cart/Cancel
        [HttpGet]
        public IActionResult Cancel()
        {
            TempData["warning-notification"] = "Payment cancelled. You can continue shopping.";
            return RedirectToAction("Index");
        }
    }
}