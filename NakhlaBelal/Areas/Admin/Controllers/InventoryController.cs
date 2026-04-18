using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Inventory
        public async Task<IActionResult> Index(string? filter = null, string? search = null, int page = 1, int pageSize = 25)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => !p.IsDeleted);

            // فلترة حسب حالة المخزون
            switch (filter)
            {
                case "out":
                    query = query.Where(p => p.ManageStock && p.StockQuantity <= 0);
                    break;
                case "low":
                    query = query.Where(p => p.ManageStock && p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold);
                    break;
                case "in":
                    query = query.Where(p => !p.ManageStock || p.StockQuantity > p.LowStockThreshold);
                    break;
                case "untracked":
                    query = query.Where(p => !p.ManageStock);
                    break;
            }

            // بحث
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.SKU.ToLower().Contains(term) ||
                    (p.Barcode != null && p.Barcode.Contains(term)));
            }

            // إحصائيات عامة (قبل التصفية بالحالة)
            var allQuery = _context.Products.Where(p => !p.IsDeleted);
            ViewBag.TotalProducts = await allQuery.CountAsync();
            ViewBag.OutOfStock = await allQuery.CountAsync(p => p.ManageStock && p.StockQuantity <= 0);
            ViewBag.LowStock = await allQuery.CountAsync(p => p.ManageStock && p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold);
            ViewBag.InStock = await allQuery.CountAsync(p => !p.ManageStock || p.StockQuantity > p.LowStockThreshold);
            ViewBag.TotalStockValue = await allQuery
                .Where(p => p.ManageStock)
                .SumAsync(p => (decimal?)(p.StockQuantity * p.CostPrice)) ?? 0;

            // Pagination
            var totalItems = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.ManageStock && p.StockQuantity <= 0 ? 0
                           : p.ManageStock && p.StockQuantity <= p.LowStockThreshold ? 1
                           : 2) // أولوية للمشاكل
                .ThenBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Filter = filter;
            ViewBag.Search = search;
            ViewBag.TotalItems = totalItems;

            return View(products);
        }

        // POST: Admin/Inventory/UpdateStock (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int productId, int newStock, int? newThreshold)
        {
            if (newStock < 0)
                return Json(new { success = false, message = "لا يمكن أن تكون الكمية سالبة" });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "المنتج غير موجود" });

            product.StockQuantity = newStock;
            product.IsInStock = newStock > 0;

            if (newThreshold.HasValue && newThreshold.Value >= 0)
                product.LowStockThreshold = newThreshold.Value;

            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            var status = !product.ManageStock ? "untracked"
                       : product.StockQuantity <= 0 ? "out"
                       : product.StockQuantity <= product.LowStockThreshold ? "low"
                       : "in";

            return Json(new
            {
                success = true,
                message = "تم تحديث المخزون ✓",
                newStock = product.StockQuantity,
                newThreshold = product.LowStockThreshold,
                status
            });
        }

        // POST: Admin/Inventory/ToggleManageStock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleManageStock(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "المنتج غير موجود" });

            product.ManageStock = !product.ManageStock;
            product.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                manageStock = product.ManageStock,
                message = product.ManageStock ? "تم تفعيل تتبع المخزون" : "تم إيقاف تتبع المخزون"
            });
        }
    }
}
