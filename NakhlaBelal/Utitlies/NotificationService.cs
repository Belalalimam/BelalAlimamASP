using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Utitlies
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task NotifyStaffOfNewOrderAsync(Order order, string? orderUrl = null)
        {
            try
            {
                // اجمع كل اليوزرز اللي عندهم role من الرولز الثلاثة — distinct عشان ما نكرر
                var staffIds = new HashSet<string>();

                foreach (var role in new[] { SD.SUPER_ADMIN_ROLE, SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE })
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                    foreach (var u in usersInRole)
                    {
                        if (!string.IsNullOrEmpty(u.Id))
                            staffIds.Add(u.Id);
                    }
                }

                if (staffIds.Count == 0)
                {
                    _logger.LogWarning("No staff users found to notify for order {OrderNumber}", order.OrderNumber);
                    return;
                }

                var title = $"طلب جديد #{order.OrderNumber}";
                var customerName = $"{order.ShippingFirstName} {order.ShippingLastName}".Trim();
                var message = $"وصل طلب جديد من {customerName} بقيمة {order.TotalAmount:0.00}.";

                var notifications = staffIds.Select(uid => new Notification
                {
                    RecipientUserId = uid,
                    Title = title,
                    Message = message,
                    Type = "NewOrder",
                    Url = orderUrl,
                    OrderId = order.Id,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                }).ToList();

                await _context.Notifications.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // لا نرمي الخطأ — ما بدنا نكسر تدفق الأوردر بسبب إشعار
                _logger.LogError(ex, "Failed to create staff notifications for order {OrderNumber}", order.OrderNumber);
            }
        }

        public async Task CreateAsync(string recipientUserId, string title, string message,
            string type = "General", string? url = null, int? orderId = null)
        {
            var n = new Notification
            {
                RecipientUserId = recipientUserId,
                Title = title,
                Message = message,
                Type = type,
                Url = url,
                OrderId = orderId,
                CreatedAt = DateTime.Now
            };
            await _context.Notifications.AddAsync(n);
            await _context.SaveChangesAsync();
        }

        public Task<int> GetUnreadCountAsync(string userId)
        {
            return _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
        }

        public async Task<IEnumerable<Notification>> GetForUserAsync(string userId, int take = 20, bool unreadOnly = false)
        {
            var q = _context.Notifications.Where(n => n.RecipientUserId == userId);
            if (unreadOnly) q = q.Where(n => !n.IsRead);
            return await q.OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var n = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientUserId == userId);
            if (n == null || n.IsRead) return;
            n.IsRead = true;
            n.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ToListAsync();
            if (unread.Count == 0) return;
            var now = DateTime.Now;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }
            await _context.SaveChangesAsync();
        }
    }
}
