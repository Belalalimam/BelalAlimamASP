using NakhlaBelal.Models;

namespace NakhlaBelal.ViewModels
{
    /// <summary>
    /// صف واحد في قائمة العملاء (Admin)
    /// </summary>
    public class CustomerListItemVM
    {
        public string Id { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? City { get; set; }
        public DateTime JoinedAt { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public bool IsBlocked { get; set; }
    }

    /// <summary>
    /// صفحة تفاصيل عميل واحد + طلباته
    /// </summary>
    public class CustomerDetailsVM
    {
        public ApplicationUser User { get; set; } = null!;
        public int OrdersCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public List<Order> Orders { get; set; } = new();
    }
}
