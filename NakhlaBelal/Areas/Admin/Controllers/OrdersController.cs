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

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

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
        public async Task<IActionResult> UpdateStatus(int orderId, string orderStatus, string paymentStatus, string adminNotes)
        {
            var orderDb = await _context.Orders.FindAsync(orderId);
            if (orderDb == null) return NotFound();

            try
            {
                orderDb.OrderStatus = orderStatus;
                orderDb.PaymentStatus = paymentStatus;
                orderDb.AdminNotes = adminNotes;
                orderDb.UpdatedAt = DateTime.Now;

                // تحديث التواريخ اللوجستية تلقائياً بناءً على الحالة
                if (orderStatus == "Processing" && orderDb.ProcessingDate == null) orderDb.ProcessingDate = DateTime.Now;
                if (orderStatus == "Shipped" && orderDb.ShippedDate == null) orderDb.ShippedDate = DateTime.Now;
                if (orderStatus == "Delivered" && orderDb.DeliveredDate == null) orderDb.DeliveredDate = DateTime.Now;
                if (orderStatus == "Cancelled" && orderDb.CancelledDate == null) orderDb.CancelledDate = DateTime.Now;

                _context.Update(orderDb);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تحديث حالة الطلب بنجاح";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "حدث خطأ أثناء التحديث: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
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