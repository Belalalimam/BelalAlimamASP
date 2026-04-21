using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class ColorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ColorsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Colors";
            ViewBag.PageTitle = "Colors";
            ViewBag.Subtitle = "Manage the palette used across products and filters";
            var colors = await _context.Colors
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(colors);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Color model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = "Please fill all required fields.";
                return RedirectToAction(nameof(Index));
            }
            _context.Colors.Add(model);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Color created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Color model)
        {
            var existing = await _context.Colors.FindAsync(model.Id);
            if (existing == null) return NotFound();
            existing.Name = model.Name;
            existing.HexCode = model.HexCode;
            existing.Description = model.Description;
            existing.ImageUrl = model.ImageUrl;
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Color updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var color = await _context.Colors.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (color == null) return NotFound();
            if (color.Products != null && color.Products.Any())
            {
                TempData["error-notification"] = $"Cannot delete — {color.Products.Count} product(s) use this color.";
                return RedirectToAction(nameof(Index));
            }
            _context.Colors.Remove(color);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Color deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
