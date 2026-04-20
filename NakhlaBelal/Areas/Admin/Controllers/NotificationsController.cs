using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NakhlaBelal.Utitlies;
using System.Security.Claims;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.SUPER_ADMIN_ROLE + "," + SD.ADMIN_ROLE + "," + SD.EMPLOYEE_ROLE)]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(bool unreadOnly = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notifications = await _notificationService.GetForUserAsync(userId, take: 100, unreadOnly: unreadOnly);
            ViewBag.UnreadOnly = unreadOnly;
            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Json(new { count = 0 });
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Json(new { items = Array.Empty<object>() });

            var items = (await _notificationService.GetForUserAsync(userId, take: 8))
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    isRead = n.IsRead,
                    url = n.Url,
                    createdAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                });
            return Json(new { items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _notificationService.MarkAsReadAsync(id, userId);
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var items = await _notificationService.GetForUserAsync(userId, take: 500);
            var n = items.FirstOrDefault(x => x.Id == id);
            if (n == null) return RedirectToAction(nameof(Index));

            await _notificationService.MarkAsReadAsync(id, userId);

            if (!string.IsNullOrEmpty(n.Url))
                return Redirect(n.Url);

            return RedirectToAction(nameof(Index));
        }
    }
}
