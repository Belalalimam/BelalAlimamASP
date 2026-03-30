using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using System.Threading.Tasks;

namespace NAKHLA.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class CategoriesController : Controller  // Fixed name: CategoriseController
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categorise
        // In CategoriesController.cs - Update Index action:
        public async Task<IActionResult> Index(int page = 1)
        {
            ViewBag.Title = "Category Management";
            ViewBag.PageTitle = "Category";
            ViewBag.Subtitle = "Manage product category";

            var categories = _context.Categorise
                .Include(c => c.Products)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .AsQueryable();

            // Fix this line - use enum, not string:
            var activeCount = categories.Count(c => c.Status == CategoryStatus.Active);
            // OR if you want to get it from Model later:
            // var activeCount = categories.ToList().Count(c => c.Status == CategoryStatus.Active);

            var totalHodsOfPage = Math.Ceiling(categories.Count() / 8.0);
            var currentPage = page;
            ViewBag.totalHodsOfPage = totalHodsOfPage;
            ViewBag.currentPage = currentPage;

            categories = categories.Skip((page - 1) * 8).Take(8);

            return View(categories.ToList());
        }

        // GET: Admin/Categories/Details/5 (for regular page)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categorise
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            // Check if it's an AJAX request (for modal)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_DetailsPartial", category);
            }

            return View(category);
        }

        // GET: Admin/Categories/GetDetails/5 (for modal - AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var category = await _context.Categorise
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return PartialView("_DetailsPartial", category);
        }

        // GET: Admin/Categorise/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categorise/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.Now;
                category.CreatedBy = User.Identity?.Name ?? "System";

                // Generate slug if empty
                if (string.IsNullOrEmpty(category.Slug))
                {
                    category.GenerateSlug();
                }

                _context.Categorise.Add(category);  // Fixed: Categorise not Categorise
                await _context.SaveChangesAsync();

                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Admin/Categorise/Edit/5 (for regular page)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categorise.FindAsync(id);  // Fixed: Categorise not Categorise
            if (category == null) return NotFound();

            return View(category);
        }

        // GET: Admin/Categorise/GetEdit/5 (for modal - AJAX)
        [HttpGet]
        public async Task<IActionResult> GetEdit(int id)
        {
            var category = await _context.Categorise.FindAsync(id);  // Fixed: Categorise not Categorise
            if (category == null) return NotFound();

            return PartialView("_EditPartial", category);
        }

        // POST: Admin/Categorise/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    category.UpdatedAt = DateTime.Now;
                    category.UpdatedBy = User.Identity?.Name ?? "System";

                    _context.Categorise.Update(category);  // Fixed: Categorise not Categorise
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Category updated successfully!";
                    return Json(new { success = true, message = "Category updated successfully!" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            // If we got this far, something failed
            return Json(new { success = false, error = "Validation failed" });
        }

        // POST: Admin/Categorise/Delete/5
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categorise.FindAsync(id);  // Fixed: Categorise not Categorise
            if (category != null)
            {
                // Soft delete
                category.IsDeleted = true;
                category.DeletedAt = DateTime.Now;
                category.DeletedBy = User.Identity?.Name ?? "System";

                _context.Categorise.Update(category);  // Fixed: Categorise not Categorise
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Category deleted successfully!" });
            }

            return Json(new { success = false, message = "Category not found!" });
        }

        // POST: Admin/Categorise/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            try
            {
                var Categorise = await _context.Categorise  // Fixed: Categorise not Categorise
                    .Where(c => ids.Contains(c.Id))
                    .ToListAsync();

                foreach (var category in Categorise)
                {
                    category.IsDeleted = true;
                    category.DeletedAt = DateTime.Now;
                    category.DeletedBy = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"{Categorise.Count} Categorise deleted" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool CategoryExists(int id)
        {
            return _context.Categorise.Any(e => e.Id == id);  // Fixed: Categorise not Categorise
        }
    }
}