using NakhlaBelal.Models;

namespace NakhlaBelal.Utitlies
{
    public interface INotificationService
    {
        /// <summary>
        /// ينشئ إشعار لكل SuperAdmin/Admin/Employee عند إنشاء طلب جديد.
        /// </summary>
        Task NotifyStaffOfNewOrderAsync(Order order, string? orderUrl = null);

        /// <summary>
        /// ينشئ إشعار لمستخدم معين.
        /// </summary>
        Task CreateAsync(string recipientUserId, string title, string message,
            string type = "General", string? url = null, int? orderId = null);

        Task<int> GetUnreadCountAsync(string userId);
        Task<IEnumerable<Notification>> GetForUserAsync(string userId, int take = 20, bool unreadOnly = false);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}
