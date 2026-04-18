using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.ViewModels;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);
            var startOf7DaysAgo = now.Date.AddDays(-6); // يشمل اليوم الحالي → 7 أيام

            // ============ أساس: الطلبات غير المحذوفة ============
            var ordersQuery = _context.Orders.Where(o => !o.IsDeleted);

            // ============ إحصائيات الطلبات ============
            var orderStats = await ordersQuery
                .GroupBy(o => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Pending = g.Count(o => o.OrderStatus == "Pending"),
                    Processing = g.Count(o => o.OrderStatus == "Processing"),
                    Shipped = g.Count(o => o.OrderStatus == "Shipped"),
                    Delivered = g.Count(o => o.OrderStatus == "Delivered"),
                    Cancelled = g.Count(o => o.OrderStatus == "Cancelled")
                })
                .FirstOrDefaultAsync();

            // ============ الإيرادات (استثناء الملغي) ============
            var revenueOrders = ordersQuery.Where(o => o.OrderStatus != "Cancelled");

            var totalRevenue = await revenueOrders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var revenueThisMonth = await revenueOrders
                .Where(o => o.CreatedAt >= startOfThisMonth)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var revenueLastMonth = await revenueOrders
                .Where(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfThisMonth)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalOrders = orderStats?.Total ?? 0;
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // ============ العملاء ============
            // كل مستخدم عمل طلب واحد على الأقل يُعتبر عميل
            var totalCustomers = await _context.Users.CountAsync();
            var newCustomersThisMonth = await _context.Users
                .CountAsync(u => u.CreatedAt >= startOfThisMonth);

            // ============ المنتجات ============
            var productsQuery = _context.Products.Where(p => !p.IsDeleted);
            var totalProducts = await productsQuery.CountAsync();
            var outOfStockProducts = await productsQuery
                .CountAsync(p => p.ManageStock && p.StockQuantity <= 0);
            var lowStockProducts = await productsQuery
                .CountAsync(p => p.ManageStock && p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold);

            // ============ المبيعات اليومية لآخر 7 أيام ============
            var dailySalesRaw = await revenueOrders
                .Where(o => o.CreatedAt >= startOf7DaysAgo)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            // ملء الأيام الفارغة بصفر
            var salesLast7Days = new List<DailySalesPoint>();
            for (int i = 0; i < 7; i++)
            {
                var day = startOf7DaysAgo.AddDays(i);
                var data = dailySalesRaw.FirstOrDefault(d => d.Date == day);
                salesLast7Days.Add(new DailySalesPoint
                {
                    Date = day,
                    Revenue = data?.Revenue ?? 0,
                    OrdersCount = data?.Count ?? 0
                });
            }

            // ============ آخر 10 طلبات ============
            var recentOrders = await ordersQuery
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            // ============ أكثر 5 منتجات مبيعاً ============
            var topProducts = await _context.OrderItems
                .Where(oi => !oi.Order.IsDeleted && oi.Order.OrderStatus != "Cancelled")
                .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.ProductImage })
                .Select(g => new TopProductItem
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ProductImage = g.Key.ProductImage,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(5)
                .ToListAsync();

            // ============ تجميع البيانات ============
            var vm = new DashboardVM
            {
                TotalOrders = totalOrders,
                PendingOrders = orderStats?.Pending ?? 0,
                ProcessingOrders = orderStats?.Processing ?? 0,
                ShippedOrders = orderStats?.Shipped ?? 0,
                DeliveredOrders = orderStats?.Delivered ?? 0,
                CancelledOrders = orderStats?.Cancelled ?? 0,

                TotalRevenue = totalRevenue,
                RevenueThisMonth = revenueThisMonth,
                RevenueLastMonth = revenueLastMonth,
                AverageOrderValue = avgOrderValue,

                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newCustomersThisMonth,

                TotalProducts = totalProducts,
                OutOfStockProducts = outOfStockProducts,
                LowStockProducts = lowStockProducts,

                SalesLast7Days = salesLast7Days,
                OrdersByStatus = new Dictionary<string, int>
                {
                    { "قيد الانتظار", orderStats?.Pending ?? 0 },
                    { "قيد التجهيز", orderStats?.Processing ?? 0 },
                    { "تم الشحن", orderStats?.Shipped ?? 0 },
                    { "تم التوصيل", orderStats?.Delivered ?? 0 },
                    { "ملغي", orderStats?.Cancelled ?? 0 }
                },
                RecentOrders = recentOrders,
                TopProducts = topProducts
            };

            return View(vm);
        }
    }
}
