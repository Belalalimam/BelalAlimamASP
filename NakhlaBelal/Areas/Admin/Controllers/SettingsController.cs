using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NakhlaBelal.Utitlies;
using NakhlaBelal.ViewModels;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE}")]
    public class SettingsController : Controller
    {
        private readonly ISiteSettingsService _settingsService;
        private readonly IWebHostEnvironment _env;

        public SettingsController(ISiteSettingsService settingsService, IWebHostEnvironment env)
        {
            _settingsService = settingsService;
            _env = env;
        }

        public IActionResult Index()
        {
            var settings = _settingsService.Get();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteSettingsVM model, IFormFile? logoFile, IFormFile? faviconFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = "يرجى تصحيح الأخطاء في النموذج";
                return View(model);
            }

            // رفع اللوجو إن وجد
            if (logoFile != null && logoFile.Length > 0)
            {
                model.LogoUrl = await SaveFileAsync(logoFile, "logo");
            }

            // رفع الفافيكون إن وجد
            if (faviconFile != null && faviconFile.Length > 0)
            {
                model.FaviconUrl = await SaveFileAsync(faviconFile, "favicon");
            }

            await _settingsService.SaveAsync(model, User.Identity?.Name);

            TempData["success-notification"] = "تم حفظ الإعدادات بنجاح ✓";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveFileAsync(IFormFile file, string namePrefix)
        {
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{namePrefix}-{Guid.NewGuid():N}{ext}";
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "settings");

            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fullPath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/settings/{fileName}";
        }
    }
}
