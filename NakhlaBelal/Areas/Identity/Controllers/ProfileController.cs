using Mapster;
//using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NakhlaBelal.DataAccess;
using NakhlaBelal.ViewModels;
using System.Threading.Tasks;

namespace NakhlaBelal.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            var userVM = user.Adapt<ApplicationUserVM>();

            // إحصائيات المستخدم الحقيقية
            var myOrders = await _context.Orders
                .Where(o => o.ApplicationUserId == user.Id && !o.IsDeleted)
                .ToListAsync();

            ViewBag.TotalOrders = myOrders.Count;
            ViewBag.TotalSpent = myOrders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.TotalAmount);
            ViewBag.DeliveredOrders = myOrders.Count(o => o.OrderStatus == "Delivered");
            ViewBag.PendingOrders = myOrders.Count(o => o.OrderStatus == "Pending" || o.OrderStatus == "Processing");
            ViewBag.MemberSince = user.CreatedAt;
            ViewBag.EmailConfirmed = user.EmailConfirmed;
            ViewBag.PhoneConfirmed = user.PhoneNumberConfirmed;

            ViewBag.RecentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.ApplicationUserId == user.Id && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(userVM);
        }

        // GET: Identity/Profile/MyOrders
        public async Task<IActionResult> MyOrders(string? status = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.ApplicationUserId == user.Id && !o.IsDeleted);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.AllCount = await _context.Orders.CountAsync(o => o.ApplicationUserId == user.Id && !o.IsDeleted);
            ViewBag.PendingCount = await _context.Orders.CountAsync(o => o.ApplicationUserId == user.Id && !o.IsDeleted && o.OrderStatus == "Pending");
            ViewBag.ProcessingCount = await _context.Orders.CountAsync(o => o.ApplicationUserId == user.Id && !o.IsDeleted && o.OrderStatus == "Processing");
            ViewBag.ShippedCount = await _context.Orders.CountAsync(o => o.ApplicationUserId == user.Id && !o.IsDeleted && o.OrderStatus == "Shipped");
            ViewBag.DeliveredCount = await _context.Orders.CountAsync(o => o.ApplicationUserId == user.Id && !o.IsDeleted && o.OrderStatus == "Delivered");
            ViewBag.CancelledCount = await _context.Orders.CountAsync(o => o.ApplicationUserId == user.Id && !o.IsDeleted && o.OrderStatus == "Cancelled");

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ApplicationUserVM applicationUserVM)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            // تحديث الحقول الجديدة يدوياً لضمان الحفظ
            user.PhoneNumber = applicationUserVM.PhoneNumber;
            user.Address = applicationUserVM.Address;
            user.City = applicationUserVM.City;       // <--- أضف هذا
            user.ZipCode = applicationUserVM.ZipCode; // <--- أضف هذا
            user.Country = applicationUserVM.Country; // <--- أضف هذا
            user.State = applicationUserVM.State;     // <--- أضف هذا

            // معالجة الاسم كما فعلت سابقاً
            var names = applicationUserVM.FullName?.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            user.FirstName = names?.Length > 0 ? names[0] : "";
            user.LastName = names?.Length > 1 ? string.Join(" ", names.Skip(1)) : "";

            await _userManager.UpdateAsync(user);
            TempData["success-notification"] = "Profile Updated!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UpdatePassword(ApplicationUserVM applicationUserVM)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user is null)
                return NotFound();

            if (applicationUserVM.CurrentPassword is null || applicationUserVM.NewPassword is null)
            {
                TempData["error-notification"] = "Must have a CurrentPassword & NewPassword value";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.ChangePasswordAsync(user, applicationUserVM.CurrentPassword, applicationUserVM.NewPassword);

            if (!result.Succeeded)
            {
                TempData["error-notification"] =
                    String.Join(", ", result.Errors.Select(e => e.Code));

                return RedirectToAction(nameof(Index));
            }

            TempData["success-notification"] = "Update Profile";
            return RedirectToAction(nameof(Index));
        }
    }
}
