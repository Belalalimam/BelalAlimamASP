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
        private readonly IWhatsAppService _whatsAppService;
        private readonly IAdminEmailNotifier _adminEmailNotifier;

        public CheckoutController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IRepository<Promotion> promotionRepository,
            ILogger<CheckoutController> logger,
            IWhatsAppService whatsAppService,
            IAdminEmailNotifier adminEmailNotifier)
        {
            _userManager = userManager;
            _context = context;
            _promotionRepository = promotionRepository;
            _logger = logger;
            _whatsAppService = whatsAppService;
            _adminEmailNotifier = adminEmailNotifier;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var cartItems = await LoadCartItemsAsync(userId);

            if (!cartItems.Any())
            {
                TempData["error-notification"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            var cartData = await BuildCartDataAsync(cartItems);

            var checkoutVM = new CheckoutVM
            {
                CartData = cartData,
                CartItems = cartItems,
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

        private async Task<List<Cart>> LoadCartItemsAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();
        }

        private decimal _ResolveShippingCost()
        {
            var raw = HttpContext.Session.GetString("ShippingCost");
            if (!string.IsNullOrEmpty(raw) &&
                decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
            return 5.99m;
        }

        private async Task<CartVM> BuildCartDataAsync(List<Cart> cartItems)
        {
            var appliedCode = HttpContext.Session.GetString("AppliedPromotionCode");
            Promotion? promotion = null;
            if (!string.IsNullOrEmpty(appliedCode))
            {
                promotion = await _promotionRepository.GetOneAsync(e => e.Code.ToLower() == appliedCode.ToLower() && e.IsActive && e.IsValid);
            }

            decimal subtotalOriginal = 0;
            decimal totalDiscount = 0;
            decimal taxTotal = 0;
            const decimal taxRate = 0.19m;

            foreach (var item in cartItems)
            {
                subtotalOriginal += item.Product.Price * item.Count;

                if (promotion != null && promotion.IsCurrentlyActive && promotion.IsApplicableToProduct(item.Product))
                {
                    decimal discountPerUnit = promotion.CalculateDiscount(item.Product.Price, 1);
                    item.Price = item.Product.Price - discountPerUnit;
                    totalDiscount += (discountPerUnit * item.Count);
                }
                else { item.Price = item.Product.Price; }

                if (item.Product.Taxable)
                {
                    taxTotal += (item.Price * item.Count) * taxRate;
                }
            }

            // Honor the shipping option the user picked in the Cart
            var shippingSessionRaw = HttpContext.Session.GetString("ShippingCost");
            decimal shippingCost = 5.99m;
            if (!string.IsNullOrEmpty(shippingSessionRaw) &&
                decimal.TryParse(shippingSessionRaw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsedShip))
            {
                shippingCost = parsedShip;
            }

            return new CartVM
            {
                CartItems = cartItems,
                Subtotal = subtotalOriginal,
                Discount = totalDiscount,
                Tax = taxTotal,
                Shipping = shippingCost,
                Total = (subtotalOriginal - totalDiscount) + taxTotal + shippingCost,
                PromotionCode = appliedCode
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(CheckoutVM model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Get cart items
            var cartItems = await LoadCartItemsAsync(userId);

            if (!cartItems.Any())
            {
                TempData["error-notification"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.CartData = await BuildCartDataAsync(cartItems);
                model.CartItems = cartItems;
                return View("Index", model);
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

                // Order amounts — honor shipping option the user picked in the cart (saved in session)
                Subtotal = cartItems.Sum(c => c.Price * c.Count),
                ShippingCost = _ResolveShippingCost(),
                TaxAmount = cartItems.Sum(c => c.Price * c.Count) * 0.08m,
                DiscountAmount = model.DiscountAmount,
                TotalAmount = cartItems.Sum(c => c.Price * c.Count) + _ResolveShippingCost() + (cartItems.Sum(c => c.Price * c.Count) * 0.08m) - model.DiscountAmount,

                // Payment info
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "Pending",

                // Order status
                OrderStatus = "Pending",

                // Customer notes
                CustomerNotes = model.CustomerNotes,

                CreatedAt = DateTime.Now
            };

            try
            {
                // 1. استخدام Transaction لضمان (يا إضافة وحذف سوا.. يا ولا شي)
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // تأكد إن الـ OrderItems مو Null قبل ما تبدأ
                        order.OrderItems = new List<OrderItem>();

                        foreach (var cartItem in cartItems)
                        {
                            var item = new OrderItem
                            {
                                ProductId = cartItem.ProductId,
                                Quantity = cartItem.Count,
                                UnitPrice = cartItem.Price,
                                TotalPrice = cartItem.Price * cartItem.Count,
                                // تأكد إن هالقيم مو null إذا الداتابيز بتطلبهم
                                ProductName = cartItem.Product?.Name ?? "N/A",
                                ProductImage = cartItem.Product?.MainImage ?? "",
                                ProductSku = cartItem.Product?.SKU ?? ""
                            };
                            order.OrderItems.Add(item);
                        }

                        // 2. إضافة الأوردر (الـ EF لحاله رح يعرف يربط الـ OrderItems ويعطيهم الـ OrderId الصح)
                        await _context.Orders.AddAsync(order);

                        // 3. حذف السلة بنفس اللحظة
                        _context.Carts.RemoveRange(cartItems);

                        // 4. تحديث بيانات عنوان اليوزر ليسترجعها بالطلبات القادمة
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            user.FirstName = model.ShippingFirstName ?? user.FirstName;
                            user.LastName = model.ShippingLastName ?? user.LastName;
                            user.Address = model.ShippingAddress ?? user.Address;
                            user.City = model.ShippingCity ?? user.City;
                            user.State = model.ShippingState ?? user.State;
                            user.ZipCode = model.ShippingZipCode ?? user.ZipCode;
                            user.Country = model.ShippingCountry ?? user.Country;
                            if (string.IsNullOrEmpty(user.PhoneNumber))
                                user.PhoneNumber = model.ShippingPhone;
                        }

                        // 5. سيف الكل بمرة وحدة فقط!
                        await _context.SaveChangesAsync();

                        // 5. تثبيت العملية
                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw; // ابعته للـ catch البرانية لنعرف شو السبب
                    }
                }

                // 6a. إيميل لكل الـ SuperAdmin/Admin/Employee بتفاصيل الطلب
                try
                {
                    await _adminEmailNotifier.NotifyNewOrderAsync(order);
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to send admin email notification for order {OrderNumber}", order.OrderNumber);
                }

                // 6. إرسال رسالة واتساب بتأكيد الطلب (لا تؤثر على تدفق الطلب إذا فشلت)
                try
                {
                    var trackingUrl = Url.Action(
                        "Index",
                        "Tracking",
                        new { area = "Customer", orderNumber = order.OrderNumber },
                        Request.Scheme) ?? string.Empty;

                    var (success, message) = await _whatsAppService.SendOrderConfirmationAsync(order, trackingUrl);
                    if (!success)
                    {
                        _logger.LogWarning("WhatsApp confirmation not sent for order {OrderNumber}: {Message}",
                            order.OrderNumber, message);
                    }
                }
                catch (Exception whatsEx)
                {
                    // سجّل الخطأ فقط؛ لا تكسر عملية الطلب
                    _logger.LogError(whatsEx, "Unexpected error sending WhatsApp confirmation for order {OrderNumber}",
                        order.OrderNumber);
                }

                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                // هون السر.. هي الرسالة رح تقولك شو الحقل اللي عم يمنع الحفظ
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError("فشل الحفظ: " + errorMessage);
                TempData["error-notification"] = "خطأ في قاعدة البيانات: " + errorMessage;

                // ارجع جيب السلة للموديل عشان ما تطلع الصفحة فاضية
                model.CartItems = cartItems;
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
        public IActionResult OrderSuccess()
        {
            return View(); // هذه الصفحة التي عرضت صورتها "Thank You"
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