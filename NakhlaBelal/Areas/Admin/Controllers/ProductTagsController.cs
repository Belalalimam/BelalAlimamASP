using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class ProductTagsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProductTagsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Product Tags";
            ViewBag.PageTitle = "Product Tags";
            ViewBag.Subtitle = "Free-form labels (trending, eco-friendly, on-sale, ...) shown on cards and filters";
            var items = await _context.ProductTags
                .Include(t => t.Products)
                .OrderBy(t => t.Name)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductTag model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = "Please fill all required fields.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = Slugify(model.Name);
            _context.ProductTags.Add(model);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Tag created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductTag model)
        {
            var existing = await _context.ProductTags.FindAsync(model.Id);
            if (existing == null) return NotFound();
            existing.Name = model.Name;
            existing.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : model.Slug;
            existing.Description = model.Description;
            existing.ImageUrl = model.ImageUrl;
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Tag updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tag = await _context.ProductTags.Include(t => t.Products).FirstOrDefaultAsync(t => t.Id == id);
            if (tag == null) return NotFound();
            // Detach all products from the tag first (many-to-many), then delete.
            if (tag.Products != null) tag.Products.Clear();
            _context.ProductTags.Remove(tag);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Tag deleted.";
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
