using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NakhlaBelal.Models;
using Stripe;
using Stripe.Checkout;
using System.Linq.Expressions;
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
        private readonly IRepository<Order> _orderRepository;
        private readonly ILogger<CartController> _logger;

        public CartController(
            UserManager<ApplicationUser> userManager,
            IRepository<Cart> cartRepository,
            IRepository<Promotion> promotionRepository,
            IProductRepository productRepository,
            IRepository<Order> orderRepository,
            ILogger<CartController> logger)
        {
            _userManager = userManager;
            _cartRepository = cartRepository;
            _promotionRepository = promotionRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        // GET: /Customer/Cart        
        [HttpGet]
        public async Task<IActionResult> Index(string? code = null, bool removePromo = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = (await _cartRepository.GetAsync(e => e.ApplicationUserId == userId, includes: [e => e.Product])).ToList();

            // جلب اليوزر الحالي (لأنه يحتوي على بيانات العنوان)
            var user = await _userManager.FindByIdAsync(userId);

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

            if (!string.IsNullOrEmpty(code))
            {
                HttpContext.Session.SetString("AppliedPromotionCode", code);
                return RedirectToAction(nameof(Index));
            }

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

            foreach (var item in cartItems)
            {
                subtotalOriginal += item.Product.Price * item.Count;
                if (item.Product.Taxable)
                {
                    taxTotal += (item.Product.Price * item.Count) * taxRate;
                }

                if (promotion != null && promotion.IsCurrentlyActive && promotion.IsApplicableToProduct(item.Product))
                {
                    decimal discountPerUnit = promotion.CalculateDiscount(item.Product.Price, 1);
                    item.Price = item.Product.Price - discountPerUnit;
                    totalDiscount += (discountPerUnit * item.Count);
                }
                else { item.Price = item.Product.Price; }
                await _cartRepository.UpdateAsync(item);
            }
            await _cartRepository.CommitAsync();
            decimal finalTotal = (subtotalOriginal - totalDiscount) + taxTotal + 5.99m;
            var vm = new CartVM
            {
                CartItems = cartItems,
                Subtotal = subtotalOriginal,
                Discount = totalDiscount,
                Tax = taxTotal,
                Shipping = 5.99m,
                Total = (subtotalOriginal - totalDiscount) + taxTotal + 5.99m,
                PromotionCode = appliedCode,
                PromotionName = promotion?.Name,

                // تعديل مهم: نتحقق إذا كان اليوزر عنده بيانات عنوان مسجلة
                // بما أن الـ VM يتوقع UserAddress كـ Object، سنقوم بتمرير اليوزر نفسه إذا كان الموديل يدعم ذلك
                // أو نستخدم خاصية النص التي أنشأناها سابقاً:
                ShippingAddress = user != null && !string.IsNullOrEmpty(user.Address)
                                  ? $"{user.Address}, {user.City}, {user.Country}"
                                  : null
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

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            // 1. جلب السلة
            var cartItems = (await _cartRepository.GetAsync(e => e.ApplicationUserId == userId, includes: [e => e.Product])).ToList();
            if (!cartItems.Any()) return RedirectToAction("Index");

            // 2. جلب الخصم من الـ Session (نفس منطق الـ Index تماماً)
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

            // 3. الحسبة الثلاثية (سعر أصلي + خصم + ضريبة)
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
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                Email = user?.Email,
                Address = user?.Address,
                City = user?.City,
                Phone = user?.PhoneNumber,
                ZipCode = user?.ZipCode,
                Country = user?.Country
            };

            return View(checkoutVM);
        }

        // GET: /Customer/Cart/Pay
        [HttpPost]
        public async Task<IActionResult> Pay()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartItems = (await _cartRepository.GetAsync(e => e.ApplicationUserId == userId, includes: [e => e.Product])).ToList();
                var user = await _userManager.FindByIdAsync(userId);

                if (!cartItems.Any()) return RedirectToAction("Index");

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = $"{Request.Scheme}://{Request.Host}/Customer/Cart/Success?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{Request.Scheme}://{Request.Host}/Customer/Cart",
                    CustomerEmail = User.FindFirstValue(ClaimTypes.Email),
                    Metadata = new Dictionary<string, string> { { "userId", userId } }
                };

                // إعداد المنتجات والضريبة والشحن (نفس كودك القديم)
                decimal taxTotal = 0;
                foreach (var item in cartItems)
                {
                    if (item.Product.Taxable)
                    {
                        taxTotal += (item.Price * item.Count) * 0.19m; // ضريبة 19% مثلاً
                    }

                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            UnitAmount = (long)(item.Price * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions { Name = item.Product.Name }
                        },
                        Quantity = item.Count
                    });
                }

                if (taxTotal > 0)
                {
                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "eur",
                            UnitAmount = (long)(taxTotal * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "VAT (19%)" }
                        },
                        Quantity = 1
                    });
                }

                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "eur",
                        UnitAmount = (long)(5.99m * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "Shipping Fee" }
                    },
                    Quantity = 1
                });

                // --- الجزء الذي كان يسبب المشكلة (تم حله بأبسط شكل) ---
                if (user != null && !string.IsNullOrEmpty(user.Address))
                {
                    options.PaymentIntentData = new SessionPaymentIntentDataOptions
                    {
                        Shipping = new ChargeShippingOptions // هذا الكلاس هو "الجوكر" في سترايب ويقبل AddressOptions
                        {
                            Name = $"{user.FirstName} {user.LastName}",
                            Address = new AddressOptions
                            {
                                Line1 = user.Address,
                                City = user.City,
                                PostalCode = user.ZipCode,
                                Country = user.Country
                            }
                        }
                    };
                }

                var service = new SessionService();
                var session = await service.CreateAsync(options);
                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pay Error");
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutVM model)
        {
            var userId = _userManager.GetUserId(User);
            var cartItems = await _cartRepository.GetAsync(
                expression: u => u.ApplicationUserId == userId,
                includes: new Expression<Func<Cart, object>>[] { c => c.Product }
            );
            var cartItemsList = cartItems.ToList();

            if (!cartItemsList.Any()) return RedirectToAction("Index", "Home");

            var order = new Order
            {
                ApplicationUserId = userId,
                CreatedAt = DateTime.Now,
                OrderNumber = "ORD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                ShippingFirstName = model.FirstName,
                ShippingLastName = model.LastName,
                ShippingAddress = model.Address,
                ShippingCity = model.City,
                ShippingZipCode = model.ZipCode,
                ShippingPhone = model.Phone,
                ShippingCountry = "Egypt", // تأكد من وجود قيمة هنا

                // مساواة حقول الـ Billing بحقول الـ Shipping لتفادي الـ Null
                BillingFirstName = model.FirstName,
                BillingLastName = model.LastName,
                BillingAddress = model.Address,
                BillingCity = model.City,
                BillingZipCode = model.ZipCode,
                BillingCountry = "Egypt",

                PaymentMethod = "Cash on Delivery",
                OrderStatus = "Pending",
                TotalAmount = cartItemsList.Sum(x => x.Price * x.Count),
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in cartItemsList)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Count,
                    UnitPrice = item.Price
                });
            }

            try
            {
                await _orderRepository.AddAsync(order);
                await _orderRepository.CommitAsync(); // لو فشل هنا هيروح للـ catch

                // المسح يتم فقط لو الحفظ نجح
                foreach (var item in cartItemsList)
                {
                    _cartRepository.Delete(item);
                }
                await _cartRepository.CommitAsync();

                return RedirectToAction(nameof(OrderSuccess));
            }
            catch (Exception ex)
            {
                // الخطوة دي هي اللي هتقولك ليه ما بيسجلش
                var error = ex.InnerException?.Message ?? ex.Message;
                ModelState.AddModelError("", "فشل الحفظ: " + error);
                return View(model); // هيرجعك لنفس الصفحة ويظهر الخطأ بدل ما يروح لصفحة النجاح كدب
            }
        }

        [HttpGet]
        public IActionResult OrderSuccess()
        {
            return View();
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