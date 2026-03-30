using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using NakhlaBelal.Utitlies;

namespace NAKHLA.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BrandsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index(int page = 1)
        {
            ViewBag.Title = "Brand Management";
            ViewBag.PageTitle = "Brands";
            ViewBag.Subtitle = "Manage product brands";

            var brands = _context.Brands.Include(e => e.Products).OrderByDescending(e => e.Status).AsQueryable();

            // Add Filters
            if (!brands.Any())
            {
                // No data in database
                ViewBag.Message = "No brands found. Add some brands first.";
            }

            var totalHodsOfPage = Math.Ceiling(brands.Count() / 8.0);
            var currentPage = page;
            ViewBag.totalHodsOfPage = totalHodsOfPage;
            ViewBag.currentPage = currentPage;

            brands = brands.Skip((page - 1) * 8).Take(8);

            return View(brands.ToList());



        }


        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(Brand brand)
        {
            if (!ModelState.IsValid)
                return View(brand);

            _context.Add(brand);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public IActionResult Edit(int id)
        {
            var brand = _context.Brands.Find(id);
            if (brand == null)
            {
                return NotFound();
            }

            // Return a partial view for modal
            return PartialView("_EditPartial", brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Website,Status,IsFeatured")] Brand brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            // Remove Products from validation
            ModelState.Remove("Products");

            // Fix for checkbox binding
            var isFeaturedValue = Request.Form["IsFeatured"].ToString();
            if (isFeaturedValue.Contains("true"))
            {
                brand.IsFeatured = true;
            }
            else
            {
                brand.IsFeatured = false;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing brand
                    var existingBrand = await _context.Brands.FindAsync(id);
                    if (existingBrand == null)
                    {
                        return NotFound();
                    }

                    // Update only specific properties
                    existingBrand.Name = brand.Name;
                    existingBrand.Description = brand.Description;
                    existingBrand.Website = brand.Website;
                    existingBrand.Status = brand.Status;
                    existingBrand.IsFeatured = brand.IsFeatured;
                    existingBrand.UpdatedDate = DateTime.Now;

                    _context.Update(existingBrand);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Brand updated successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, error = ex.Message });
                }
            }

            // Return validation errors
            return PartialView("_EditPartial", brand);
        }
        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.Id == id);
        }
        //[HttpGet]
        //[HttpPost]



        [HttpGet]
        public IActionResult Details(int id)
        {
            // Find the brand by ID
            var brand = _context.Brands
                .FirstOrDefault(b => b.Id == id);

            if (brand == null)
            {
                // Return 404 if brand not found
                return NotFound();
            }

            // Return a partial view for modal
            return PartialView("_DetailsPartial", brand);
        }



        // POST: /Admin/Brands/Delete/5
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null)
                {
                    return Json(new { success = false, message = "Brand not found." });
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Brand '{brand.Name}' deleted successfully.",
                    id = id
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error deleting brand: {ex.Message}"
                });
            }
        }

        // For bulk deletion
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            try
            {
                var brands = _context.Brands.Where(b => ids.Contains(b.Id)).ToList();

                if (!brands.Any())
                {
                    return Json(new { success = false, message = "No brands found to delete." });
                }

                // Check for brands with products
                var brandsWithProducts = brands.Where(b => b.Products != null && b.Products.Any()).ToList();
                if (brandsWithProducts.Any())
                {
                    var brandNames = string.Join(", ", brandsWithProducts.Select(b => b.Name));
                    return Json(new
                    {
                        success = false,
                        message = $"Cannot delete brands with associated products: {brandNames}"
                    });
                }

                _context.Brands.RemoveRange(brands);
                await _context.SaveChangesAsync();  

                var deletedNames = string.Join(", ", brands.Select(b => b.Name));
                return Json(new
                {
                    success = true,
                    message = $"Deleted {brands.Count} brands: {deletedNames}",
                    ids = ids
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error deleting brands: {ex.Message}"
                });
            }
        }

    }
}
