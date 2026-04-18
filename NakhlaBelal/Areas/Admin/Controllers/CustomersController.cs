using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.ViewModels;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Customers
        public async Task<IActionResult> Index(string? search = null, string? sort = "recent", int page = 1, int pageSize = 20)
        {
            // هات كل المستخدمين بدور Customer فقط
            var customerRoleUsers = await _userManager.GetUsersInRoleAsync(SD.CUSTOMER_ROLE);
            var customerIds = customerRoleUsers.Select(u => u.Id).ToList();

            // ابنِ استعلام الطلبات لجميع العملاء دفعة واحدة
            var ordersAgg = await _context.Orders
                .Where(o => !o.IsDeleted && customerIds.Contains(o.ApplicationUserId))
                .GroupBy(o => o.ApplicationUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrdersCount = g.Count(),
                    TotalSpent = g.Where(x => x.OrderStatus != "Cancelled").Sum(x => (decimal?)x.TotalAmount) ?? 0,
                    LastOrderDate = g.Max(x => x.CreatedAt)
                })
                .ToDictionaryAsync(x => x.UserId);

            // حوّل إلى VM
            var customers = customerRoleUsers.Select(u =>
            {
                ordersAgg.TryGetValue(u.Id, out var agg);
                return new CustomerListItemVM
                {
                    Id = u.Id,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    City = u.City,
                    JoinedAt = u.CreatedAt,
                    OrdersCount = agg?.OrdersCount ?? 0,
                    TotalSpent = agg?.TotalSpent ?? 0,
                    LastOrderDate = agg?.LastOrderDate,
                    IsBlocked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
                };
            }).AsQueryable();

            // بحث
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                customers = customers.Where(c =>
                    (c.FullName != null && c.FullName.ToLower().Contains(term)) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)) ||
                    (c.PhoneNumber != null && c.PhoneNumber.Contains(term)));
            }

            // ترتيب
            customers = sort switch
            {
                "name" => customers.OrderBy(c => c.FullName),
                "spent" => customers.OrderByDescending(c => c.TotalSpent),
                "orders" => customers.OrderByDescending(c => c.OrdersCount),
                _ => customers.OrderByDescending(c => c.JoinedAt)
            };

            // إحصائيات
            var list = customers.ToList();
            ViewBag.TotalCustomers = list.Count;
            ViewBag.ActiveCustomers = list.Count(c => c.OrdersCount > 0);
            ViewBag.BlockedCustomers = list.Count(c => c.IsBlocked);
            ViewBag.TotalRevenue = list.Sum(c => c.TotalSpent);

            // Pagination
            var totalPages = (int)Math.Ceiling(list.Count / (double)pageSize);
            var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            return View(paged);
        }

        // GET: Admin/Customers/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var orders = await _context.Orders
                .Where(o => !o.IsDeleted && o.ApplicationUserId == id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var totalSpent = orders.Where(o => o.OrderStatus != "Cancelled").Sum(o => o.TotalAmount);
            var validCount = orders.Count(o => o.OrderStatus != "Cancelled");

            var vm = new CustomerDetailsVM
            {
                User = user,
                OrdersCount = orders.Count,
                TotalSpent = totalSpent,
                AverageOrderValue = validCount > 0 ? totalSpent / validCount : 0,
                LastOrderDate = orders.FirstOrDefault()?.CreatedAt,
                Orders = orders
            };

            return View(vm);
        }

        // POST: Admin/Customers/ToggleBlock/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // لا تسمح بحظر مسؤولين
            if (await _userManager.IsInRoleAsync(user, SD.SUPER_ADMIN_ROLE) ||
                await _userManager.IsInRoleAsync(user, SD.ADMIN_ROLE))
            {
                TempData["error-notification"] = "لا يمكن حظر حساب مسؤول";
                return RedirectToAction(nameof(Index));
            }

            var isBlocked = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;

            if (isBlocked)
            {
                user.LockoutEnd = null;
                TempData["success-notification"] = $"تم إلغاء حظر {user.FirstName} {user.LastName}";
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // حظر فعلي
                TempData["success-notification"] = $"تم حظر {user.FirstName} {user.LastName}";
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
