using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    /// <summary>
    /// إدارة مستخدمي لوحة التحكم (Admin / Employee / SuperAdmin)
    /// ملاحظة: العملاء يُعرضون في CustomersController منفصلاً
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Index(string? role = null, string? search = null)
        {
            // نريد كل المستخدمين الذين عندهم دور إداري (Admin, Employee, SuperAdmin)
            var superAdmins = await _userManager.GetUsersInRoleAsync(SD.SUPER_ADMIN_ROLE);
            var admins = await _userManager.GetUsersInRoleAsync(SD.ADMIN_ROLE);
            var employees = await _userManager.GetUsersInRoleAsync(SD.EMPLOYEE_ROLE);

            var staffUsers = superAdmins
                .Concat(admins)
                .Concat(employees)
                .DistinctBy(u => u.Id)
                .ToList();

            // فلترة بالدور
            if (!string.IsNullOrEmpty(role))
            {
                var roleUsers = await _userManager.GetUsersInRoleAsync(role);
                staffUsers = staffUsers.Where(u => roleUsers.Any(r => r.Id == u.Id)).ToList();
            }

            // بحث
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                staffUsers = staffUsers.Where(u =>
                    (u.FirstName?.ToLower().Contains(term) ?? false) ||
                    (u.LastName?.ToLower().Contains(term) ?? false) ||
                    (u.Email?.ToLower().Contains(term) ?? false)).ToList();
            }

            // تجهيز ViewModel — يحتاج كل مستخدم دوره
            var userList = new List<UserRowVM>();
            foreach (var u in staffUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userList.Add(new UserRowVM
                {
                    User = u,
                    Roles = roles.ToList(),
                    IsBlocked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
                });
            }

            ViewBag.TotalStaff = userList.Count;
            ViewBag.SuperAdminsCount = superAdmins.Count;
            ViewBag.AdminsCount = admins.Count;
            ViewBag.EmployeesCount = employees.Count;
            ViewBag.CurrentRole = role;
            ViewBag.Search = search;
            ViewBag.AllRoles = _roleManager.Roles
                .Where(r => r.Name != SD.CUSTOMER_ROLE)
                .Select(r => r.Name)
                .ToList();

            return View(userList);
        }

        // POST: Admin/Users/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
                return Json(new { success = false, message = "بيانات ناقصة" });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "المستخدم غير موجود" });

            // منع تغيير دور Super Admin إلا من نفسه
            if (await _userManager.IsInRoleAsync(user, SD.SUPER_ADMIN_ROLE) && user.Id != _userManager.GetUserId(User))
            {
                return Json(new { success = false, message = "لا يمكن تعديل دور SuperAdmin آخر" });
            }

            // احذف كل الأدوار الإدارية الحالية
            var adminRoles = new[] { SD.SUPER_ADMIN_ROLE, SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE };
            var currentRoles = await _userManager.GetRolesAsync(user);
            var toRemove = currentRoles.Where(r => adminRoles.Contains(r)).ToList();
            if (toRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, toRemove);

            // أضف الدور الجديد
            await _userManager.AddToRoleAsync(user, newRole);

            return Json(new { success = true, message = $"تم تغيير دور {user.FirstName} إلى {newRole}" });
        }

        // POST: Admin/Users/ToggleBlock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Super Admin لا يمكن حظره
            if (await _userManager.IsInRoleAsync(user, SD.SUPER_ADMIN_ROLE))
            {
                TempData["error-notification"] = "لا يمكن حظر حساب Super Admin";
                return RedirectToAction(nameof(Index));
            }

            // Admin لا يقدر يحظر Admin آخر (فقط Super Admin يقدر)
            if (await _userManager.IsInRoleAsync(user, SD.ADMIN_ROLE) && !User.IsInRole(SD.SUPER_ADMIN_ROLE))
            {
                TempData["error-notification"] = "فقط Super Admin يمكنه حظر حساب Admin";
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
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                TempData["success-notification"] = $"تم حظر {user.FirstName} {user.LastName}";
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/Invite
        [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
        public IActionResult Invite()
        {
            ViewBag.Roles = new SelectList(new[] { SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE });
            return View();
        }

        // POST: Admin/Users/Invite
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.SUPER_ADMIN_ROLE)]
        public async Task<IActionResult> Invite(InviteUserVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(new[] { SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE });
                return View(model);
            }

            // تحقق من عدم التكرار
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "هذا البريد مستخدم بالفعل");
                ViewBag.Roles = new SelectList(new[] { SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE });
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true, // المدعو يتفعّل مباشرة
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.TempPassword);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError("", err.Description);

                ViewBag.Roles = new SelectList(new[] { SD.ADMIN_ROLE, SD.EMPLOYEE_ROLE });
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            TempData["success-notification"] = $"تم إنشاء حساب {model.Email} بدور {model.Role} بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }

    // ============ ViewModels داخل نفس الملف لتوحيد المكان ============
    public class UserRowVM
    {
        public ApplicationUser User { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
        public bool IsBlocked { get; set; }
    }

    public class InviteUserVM
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "الاسم الأول مطلوب")]
        public string FirstName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "الاسم الأخير مطلوب")]
        public string LastName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "البريد مطلوب")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "بريد غير صحيح")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "كلمة المرور المؤقتة مطلوبة")]
        [System.ComponentModel.DataAnnotations.MinLength(8, ErrorMessage = "8 أحرف على الأقل")]
        public string TempPassword { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "اختر الدور")]
        public string Role { get; set; } = SD.EMPLOYEE_ROLE;
    }
}
