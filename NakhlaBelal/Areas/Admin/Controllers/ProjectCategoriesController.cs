using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class ProjectCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProjectCategoriesController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Project Categories";
            ViewBag.PageTitle = "Project Categories";
            ViewBag.Subtitle = "Project themes (Dress, Coat, Jacket, ...) shown on the homepage and used to filter fabrics by use-case";
            var items = await _context.ProjectCategories
                .Include(p => p.Products)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectCategory model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = "Please fill all required fields.";
                return RedirectToAction(nameof(Index));
            }
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = Slugify(model.Name);
            _context.ProjectCategories.Add(model);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Project category created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectCategory model)
        {
            var existing = await _context.ProjectCategories.FindAsync(model.Id);
            if (existing == null) return NotFound();
            existing.Name = model.Name;
            existing.Slug = string.IsNullOrWhiteSpace(model.Slug) ? Slugify(model.Name) : model.Slug;
            existing.Icon = model.Icon;
            existing.Description = model.Description;
            existing.ImageUrl = model.ImageUrl;
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Project category updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ProjectCategories.Include(p => p.Products).FirstOrDefaultAsync(p => p.Id == id);
            if (item == null) return NotFound();
            // Many-to-many with Products: clear join first
            if (item.Products != null) item.Products.Clear();
            _context.ProjectCategories.Remove(item);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Project category deleted.";
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
