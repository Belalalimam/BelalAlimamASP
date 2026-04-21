using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class CompositionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CompositionsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Compositions";
            ViewBag.PageTitle = "Compositions";
            ViewBag.Subtitle = "Fibres you can mix on a product (Cotton, Polyester, ...)";
            var items = await _context.Compositions.OrderBy(c => c.Name).ToListAsync();

            // Count usages via ProductCompositions join
            var usage = await _context.ProductCompositions
                .GroupBy(pc => pc.CompositionId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
            ViewBag.Usage = usage;

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Composition model)
        {
            if (!ModelState.IsValid)
            {
                TempData["error-notification"] = "Please fill all required fields.";
                return RedirectToAction(nameof(Index));
            }
            _context.Compositions.Add(model);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Composition created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Composition model)
        {
            var existing = await _context.Compositions.FindAsync(model.Id);
            if (existing == null) return NotFound();
            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.ImageUrl = model.ImageUrl;
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Composition updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var usedBy = await _context.ProductCompositions.CountAsync(pc => pc.CompositionId == id);
            if (usedBy > 0)
            {
                TempData["error-notification"] = $"Cannot delete — used by {usedBy} product(s).";
                return RedirectToAction(nameof(Index));
            }
            var composition = await _context.Compositions.FindAsync(id);
            if (composition == null) return NotFound();
            _context.Compositions.Remove(composition);
            await _context.SaveChangesAsync();
            TempData["success-notification"] = "Composition deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
