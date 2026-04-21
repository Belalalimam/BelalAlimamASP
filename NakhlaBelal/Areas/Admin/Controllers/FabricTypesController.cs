using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class FabricTypesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public FabricTypesController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Fabric Types";
            ViewBag.PageTitle = "Fabric Types";
            ViewBag.Subtitle = "Broad fabric families (Cotton, Silk, Lace, ...)";
            var items = await _context.FabricTypes
                .Include(f => f.Products)
                .OrderBy(f => f.Name)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FabricType model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = "Please fill all required fields.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = Slugify(model.Name);
            _context.FabricTypes.Add(model);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Fabric type created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FabricType model)
        {
            var existing = await _context.FabricTypes.FindAsync(model.Id);
            if (existing == null) return NotFound();
            existing.Name = model.Name;
            existing.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : model.Slug;
            existing.Description = model.Description;
            existing.ImageUrl = model.ImageUrl;
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Fabric type updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var fabricType = await _context.FabricTypes.Include(f => f.Products).FirstOrDefaultAsync(f => f.Id == id);
            if (fabricType == null) return NotFound();
            if (fabricType.Products != null && fabricType.Products.Any())
            {
                TempData["error-notification"] = $"Cannot delete — {fabricType.Products.Count} product(s) use this type.";
                return RedirectToAction(nameof(Index));
            }
            _context.FabricTypes.Remove(fabricType);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Fabric type deleted.";
            return RedirectToAction(nameof(Index));
        }

        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim().ToLowerInvariant();
            var sb = new System.Text.StringBuilder();
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (ch == ' ' || ch == '-' || ch == '_') sb.Append('-');
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        }
    }
}
