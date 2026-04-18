using NakhlaBelal.Models;

namespace NakhlaBelal.ViewModels
{
    /// <summary>
    /// ViewModel للوحة التحكم الرئيسية في الـ Admin
    /// يجمع الإحصائيات العامة + بيانات الرسوم البيانية + آخر الطلبات والمنتجات الأكثر مبيعاً
    /// </summary>
    public class DashboardVM
    {
        // ============ الإحصائيات الرئيسية ============
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal AverageOrderValue { get; set; }

        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }

        public int TotalProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }

        // ============ بيانات الرسوم البيانية ============
        // مبيعات آخر 7 أيام (التاريخ → الإيراد)
        public List<DailySalesPoint> SalesLast7Days { get; set; } = new();

        // عدد الطلبات لكل حالة (للـ Doughnut Chart)
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();

        // ============ جداول ============
        public List<Order> RecentOrders { get; set; } = new();
        public List<TopProductItem> TopProducts { get; set; } = new();
    }

    /// <summary>
    /// نقطة في مخطط المبيعات اليومية
    /// </summary>
    public class DailySalesPoint
    {
        public DateTime Date { get; set; }
        public string Label => Date.ToString("dd/MM");
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
    }

    /// <summary>
    /// عنصر في قائمة أكثر المنتجات مبيعاً
    /// </summary>
    public class TopProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
