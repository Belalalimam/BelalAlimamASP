using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    /// <summary>
    /// إدارة المدفوعات — يعرض كل الطلبات من منظور الدفع
    /// (مدفوع / معلّق / فشل / مسترد) ويوفّر إجراءات مثل تعليم كمدفوع أو استرجاع
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Payments
        public async Task<IActionResult> Index(
            string? status = null,
            string? method = null,
            string? search = null,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 25)
        {
            var query = _context.Orders
                .Include(o => o.ApplicationUser)
                .Where(o => !o.IsDeleted);

            // فلترة بحالة الدفع
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.PaymentStatus == status);

            // فلترة بطريقة الدفع
            if (!string.IsNullOrEmpty(method))
                query = query.Where(o => o.PaymentMethod == method);

            // فلترة بالتاريخ
            if (from.HasValue)
                query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(o => o.CreatedAt <= to.Value.AddDays(1));

            // بحث برقم الطلب / رقم المعاملة / اسم العميل
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderNumber.ToLower().Contains(term) ||
                    (o.PaymentTransactionId != null && o.PaymentTransactionId.ToLower().Contains(term)) ||
                    (o.ShippingFirstName != null && o.ShippingFirstName.ToLower().Contains(term)) ||
                    (o.ShippingLastName != null && o.ShippingLastName.ToLower().Contains(term)) ||
                    (o.ShippingPhone != null && o.ShippingPhone.Contains(term)));
            }

            // إحصائيات عامة (قبل فلتر الحالة)
            var allOrders = _context.Orders.Where(o => !o.IsDeleted);
            ViewBag.TotalRevenue = await allOrders.Where(o => o.PaymentStatus == "Paid").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.PendingAmount = await allOrders.Where(o => o.PaymentStatus == "Pending").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.RefundedAmount = await allOrders.Where(o => o.PaymentStatus == "Refunded").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.FailedAmount = await allOrders.Where(o => o.PaymentStatus == "Failed").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.PaidCount = await allOrders.CountAsync(o => o.PaymentStatus == "Paid");
            ViewBag.PendingCount = await allOrders.CountAsync(o => o.PaymentStatus == "Pending");
            ViewBag.RefundedCount = await allOrders.CountAsync(o => o.PaymentStatus == "Refunded");
            ViewBag.FailedCount = await allOrders.CountAsync(o => o.PaymentStatus == "Failed");

            // طرق الدفع المتاحة
            ViewBag.PaymentMethods = await _context.Orders
                .Where(o => !o.IsDeleted && o.PaymentMethod != null)
                .Select(o => o.PaymentMethod)
                .Distinct()
                .ToListAsync();

            // Pagination
            var totalItems = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Status = status;
            ViewBag.Method = method;
            ViewBag.Search = search;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.TotalItems = totalItems;

            return View(orders);
        }

        // POST: Admin/Payments/MarkAsPaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["error-notification"] = "الطلب غير موجود";
                return RedirectToAction(nameof(Index));
            }

            order.PaymentStatus = "Paid";
            order.PaymentDate = DateTime.Now;
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["success-notification"] = $"تم تعليم الطلب {order.OrderNumber} كمدفوع";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Payments/Refund
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
        public async Task<IActionResult> Refund(int id, string? reason = null)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["error-notification"] = "الطلب غير موجود";
                return RedirectToAction(nameof(Index));
            }

            if (order.PaymentStatus != "Paid")
            {
                TempData["error-notification"] = "لا يمكن استرجاع طلب غير مدفوع";
                return RedirectToAction(nameof(Index));
            }

            order.PaymentStatus = "Refunded";
            order.AdminNotes = string.IsNullOrEmpty(order.AdminNotes)
                ? $"استرجاع: {reason}"
                : $"{order.AdminNotes}\nاسترجاع: {reason}";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["success-notification"] = $"تم استرجاع مبلغ الطلب {order.OrderNumber}";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Payments/MarkAsFailed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsFailed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["error-notification"] = "الطلب غير موجود";
                return RedirectToAction(nameof(Index));
            }

            order.PaymentStatus = "Failed";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["success-notification"] = $"تم تعليم الطلب {order.OrderNumber} كفاشل";
            return RedirectToAction(nameof(Index));
        }
    }
}
