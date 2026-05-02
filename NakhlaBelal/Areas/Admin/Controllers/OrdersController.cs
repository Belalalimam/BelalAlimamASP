using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NAKHLA.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext context,
            IWhatsAppService whatsAppService,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        // =========================================================
        // GET: Admin/Orders/TestWhatsApp?phone=201xxxxxxxxx
        // صفحة اختبار سريعة لإرسال رسالة قالب تجريبية
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> TestWhatsApp(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return Content(
                    "أضف رقم الهاتف على الرابط هكذا:\n" +
                    "/Admin/Orders/TestWhatsApp?phone=201XXXXXXXXX\n" +
                    "(ابدأ بكود الدولة بدون + ولا 00)\n\n" +
                    "لاختبار قالب hello_world الافتراضي المعتمد من Meta:\n" +
                    "/Admin/Orders/TestWhatsApp?phone=201XXXXXXXXX&mode=hello",
                    "text/plain; charset=utf-8");
            }

            // mode=hello → قالب hello_world الافتراضي (معتمد تلقائياً من Meta)
            // mode=order (الافتراضي) → قالب order_confirmation (يتطلب إنشاءه واعتماده في Meta)
            var mode = Request.Query["mode"].ToString().ToLowerInvariant();

            bool success;
            string message;
            string templateUsed;

            if (mode == "hello")
            {
                (success, message) = await _whatsAppService.SendHelloWorldAsync(phone);
                templateUsed = "hello_world (en_US)";
            }
            else
            {
                var fakeOrder = new Order
                {
                    OrderNumber = "TEST-" + DateTime.Now.ToString("HHmmss"),
                    ShippingPhone = phone,
                    TotalAmount = 100m,
                    ShippingFirstName = "عميل",
                    ShippingLastName = "اختبار"
                };

                var trackingUrl = Url.Action("Index", "Tracking",
                    new { area = "Customer", orderNumber = fakeOrder.OrderNumber },
                    Request.Scheme) ?? "https://example.com";

                (success, message) = await _whatsAppService.SendOrderConfirmationAsync(fakeOrder, trackingUrl);
                templateUsed = "order_confirmation (ar)";
            }

            var result = $"Success: {success}\n\nResponse:\n{message}\n\n" +
                         $"Phone input: '{phone}'\n" +
                         $"Template: {templateUsed}\n" +
                         $"Mode: {(string.IsNullOrEmpty(mode) ? "order (default)" : mode)}";

            return Content(result, "text/plain; charset=utf-8");
        }

        // تحويل حالة الطلب الإنجليزية إلى نص عربي لرسائل واتساب
        private static string GetArabicStatus(string status) => status switch
        {
            "Pending" => "قيد الانتظار",
            "Processing" => "قيد التجهيز",
            "Shipped" => "تم الشحن",
            "Delivered" => "تم التوصيل",
            "Cancelled" => "ملغي",
            _ => status
        };

        // GET: Admin/Orders
        public async Task<IActionResult> Index(string status = null, int page = 1, int pageSize = 10)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.ApplicationUser)
                .Where(o => !o.IsDeleted);

            // فلترة حسب حالة الطلب إذا تم اختيارها
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var totalOrders = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // حساب الإحصائيات (نفس ستايل البرودكت)
            var stats = new
            {
                Total = await _context.Orders.CountAsync(o => !o.IsDeleted),
                Pending = await _context.Orders.CountAsync(o => !o.IsDeleted && o.OrderStatus == "Pending"),
                Shipped = await _context.Orders.CountAsync(o => !o.IsDeleted && o.OrderStatus == "Shipped"),
                Delivered = await _context.Orders.CountAsync(o => !o.IsDeleted && o.OrderStatus == "Delivered"),
                Revenue = await _context.Orders.Where(o => !o.IsDeleted && o.OrderStatus != "Cancelled").SumAsync(o => o.TotalAmount)
            };

            ViewBag.Stats = stats;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Admin/Orders/GetDetails/5 (للعرض السريع AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

            if (order == null) return NotFound();

            return PartialView("_OrderDetailsPartial", order);
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string orderStatus, string paymentStatus, string adminNotes, string? paymentMethod = null, string? paymentTransactionId = null, string? carrier = null, string? trackingNumber = null)
        {
            // ابحث عن الطلب من الداتابيز مباشرة
            var orderDb = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (orderDb == null) return Json(new { success = false, message = "الطلب غير موجود" });

            try
            {
                // احفظ الحالة القديمة لمعرفة هل فعلاً تغيّرت
                var previousStatus = orderDb.OrderStatus;
                var previousPaymentStatus = orderDb.PaymentStatus;

                // نعدل القيم يدوياً على الكائن الذي يتم تتبعه حالياً
                orderDb.OrderStatus = orderStatus;
                orderDb.PaymentStatus = paymentStatus;
                orderDb.AdminNotes = adminNotes;
                orderDb.UpdatedAt = DateTime.Now;

                // تحديث طريقة الدفع ومعرّف المعاملة (إن تم تمريرها)
                if (!string.IsNullOrWhiteSpace(paymentMethod))
                    orderDb.PaymentMethod = paymentMethod;
                if (paymentTransactionId != null)
                    orderDb.PaymentTransactionId = paymentTransactionId;
                if (!string.IsNullOrWhiteSpace(carrier))
                    orderDb.Carrier = carrier;
                if (!string.IsNullOrWhiteSpace(trackingNumber))
                    orderDb.TrackingNumber = trackingNumber;

                // تسجيل تاريخ الدفع تلقائياً عند تحويل الحالة لـ Paid
                if (paymentStatus == "Paid" && previousPaymentStatus != "Paid" && orderDb.PaymentDate == null)
                    orderDb.PaymentDate = DateTime.Now;

                // تحديث التواريخ تلقائياً (منطقك السابق ممتاز)
                if (orderStatus == "Processing" && orderDb.ProcessingDate == null) orderDb.ProcessingDate = DateTime.Now;
                if (orderStatus == "Shipped" && orderDb.ShippedDate == null) orderDb.ShippedDate = DateTime.Now;
                if (orderStatus == "Delivered" && orderDb.DeliveredDate == null) orderDb.DeliveredDate = DateTime.Now;

                // لا داعي لمناداة Update(orderDb) لأن EF يتابع التغييرات تلقائياً هنا
                await _context.SaveChangesAsync();

                // إرسال إشعار واتساب فقط إذا تغيّرت حالة الطلب فعلياً
                if (!string.Equals(previousStatus, orderStatus, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var trackingUrl = Url.Action(
                            "Index",
                            "Tracking",
                            new { area = "Customer", orderNumber = orderDb.OrderNumber },
                            Request.Scheme) ?? string.Empty;

                        var (success, message) = await _whatsAppService.SendOrderStatusUpdateAsync(
                            orderDb, GetArabicStatus(orderStatus), trackingUrl);

                        if (!success)
                        {
                            _logger.LogWarning("WhatsApp status update not sent for order {OrderNumber}: {Message}",
                                orderDb.OrderNumber, message);
                        }
                    }
                    catch (Exception whatsEx)
                    {
                        _logger.LogError(whatsEx, "Unexpected error sending WhatsApp status update for order {OrderNumber}",
                            orderDb.OrderNumber);
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // عشان تعرف شو السبب الحقيقي للخطأ، جرب تطبع الـ InnerException
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = msg });
            }
        }

        // POST: Admin/Orders/Delete/5 (Soft Delete)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return Json(new { success = false, message = "الطلب غير موجود" });

                order.IsDeleted = true;
                order.DeletedAt = DateTime.Now;
                // هنا نفترض وجود UpdatedBy أو يمكنك إضافة DeletedBy للموديل مستقبلاً

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم حذف الطلب بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Admin/Orders/DeleteMultiple (كما في المنتجات)
        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any()) return Json(new { success = false, message = "لم يتم اختيار طلبات" });

                var orders = await _context.Orders.Where(o => ids.Contains(o.Id)).ToListAsync();
                foreach (var order in orders)
                {
                    order.IsDeleted = true;
                    order.DeletedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"تم حذف {orders.Count} طلب بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}