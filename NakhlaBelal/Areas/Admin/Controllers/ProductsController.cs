using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NAKHLA.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Color)
                .Include(p => p.FabricType)
                .Include(p => p.ProjectCategories)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalProducts = await _context.Products.CountAsync(p => !p.IsDeleted);
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            ViewBag.TotalProducts = totalProducts;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            // Calculate stats
            var stats = new
            {
                TotalProducts = totalProducts,
                InStock = await _context.Products.CountAsync(p => !p.IsDeleted && p.StockQuantity > 0),
                OutOfStock = await _context.Products.CountAsync(p => !p.IsDeleted && p.StockQuantity == 0),
                LowStock = await _context.Products.CountAsync(p => !p.IsDeleted && p.StockQuantity <= 10 && p.StockQuantity > 0)
            };

            ViewBag.Stats = stats;

            return View(products);
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.FabricType)
                .Include(p => p.Color)
                .Include(p => p.ProjectCategories)
                .Include(p => p.ProductTags)
                .Include(p => p.ProductCompositions) 
            .ThenInclude(pc => pc.Composition)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Admin/Products/GetDetails/5
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.FabricType)
                .Include(p => p.Color)
                .Include(p => p.ProjectCategories)
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            return PartialView("_DetailsPartial", product);
        }

        // GET: Admin/Products/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Get active categories
            var categories = await _context.Categorise
                .Where(c => c.Status == CategoryStatus.Active)
                .ToListAsync();

            // Get active brands
            var brands = await _context.Brands
                .Where(b => b.Status == "Active")
                .ToListAsync();
            ViewBag.Compositions = await _context.Compositions.ToListAsync();
            ViewBag.ProductColors = await _context.Colors.ToListAsync();
            var ProductTags = await _context.ProductTags.ToListAsync();
            var colors = await _context.Colors.ToListAsync();

            var fabricTypes = await _context.FabricTypes.ToListAsync();

            var projectCategories = await _context.ProjectCategories.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;
            ViewBag.ProductTags = ProductTags;
            ViewBag.Colors = colors;
            ViewBag.FabricTypes = fabricTypes;
            ViewBag.ProjectCategories = projectCategories;

            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile img, List<IFormFile>? subImgs, int[] SelectedProjectCategoryIds, List<int> SelectedTagIds, List<ProductComposition> ProductCompositions)
        {
            // Debug: Check what's coming in
            Console.WriteLine($"Product Name: {product?.Name}");
            Console.WriteLine($"Product SKU: {product?.SKU}");
            Console.WriteLine($"Product Price: {product?.Price}");
            Console.WriteLine($"Product CategoryId: {product?.CategoryId}");

            if (ModelState.IsValid)
            {
                try
                {
                    // Set defaults
                    product.CreatedAt = DateTime.Now;
                    product.CreatedBy = User.Identity?.Name ?? "System";
                    product.IsDeleted = false;

                    if (img is not null && img.Length > 0)
                    {
                        // Save Img in wwwroot
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(img.FileName); // 30291jsfd4-210klsdf32-4vsfksgs.png
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images", fileName);

                        using (var stream = System.IO.File.Create(filePath))
                        {
                            img.CopyTo(stream);
                        }

                        // Save Img in db
                        product.MainImage = fileName;
                    }

                    // Generate SKU if empty
                    if (string.IsNullOrEmpty(product.SKU))
                    {
                        product.SKU = "PROD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                    }

                    if (SelectedProjectCategoryIds != null && SelectedProjectCategoryIds.Any())
                    {
                        product.ProjectCategories = new List<ProjectCategory>();
                        foreach (var id in SelectedProjectCategoryIds)
                        {
                            var category = await _context.ProjectCategories.FindAsync(id);
                            if (category != null)
                                product.ProjectCategories.Add(category);
                        }
                    }
                    if (SelectedTagIds != null)
                    {
                        var tags = _context.ProductTags
                            .Where(t => SelectedTagIds.Contains(t.Id))
                            .ToList();

                        product.ProductTags = tags;
                    }


                    if (ProductCompositions != null && ProductCompositions.Any())
                    {
                        product.ProductCompositions = new List<ProductComposition>();
                        foreach (var item in ProductCompositions)
                        {
                            if (item.CompositionId > 0 && item.Percentage > 0)
                            {
                                // لا نحتاج لتعيين ProductId هنا لأن EF سيربطها تلقائياً عند إضافة المنتج
                                product.ProductCompositions.Add(item);
                            }
                        }
                    }


                    // Generate slug if empty
                    if (string.IsNullOrEmpty(product.Slug))
                    {
                        product.Slug = GenerateSlug(product.Name);
                    }

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    

                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the error
                    Console.WriteLine($"Error saving product: {ex.Message}");
                    ModelState.AddModelError("", $"Error saving product: {ex.Message}");
                }
            }
            else
            {
                // Show validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");

                // Add errors to ViewBag to display in view
                ViewBag.ValidationErrors = errors.ToList();
            }

            // If validation fails, reload dropdowns
            // Use _context.Categorise (not Categories)
            var categories = await _context.Categorise
                .Where(c => c.Status == CategoryStatus.Active)
                .ToListAsync();

            var brands = await _context.Brands
                .Where(b => b.Status == "Active")
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Brands = brands;


            ViewBag.FabricTypes = await _context.FabricTypes.ToListAsync();
            ViewBag.ProjectCategories = await _context.ProjectCategories.ToListAsync();
            ViewBag.ProductTags = await _context.ProductTags.ToListAsync();   // هذا يحل خطأ سطر 214
            ViewBag.Colors = await _context.Colors.ToListAsync();

            ViewBag.Compositions = await _context.Compositions.ToListAsync();

            return View(product);
        }


        [HttpPost]
        public async Task<IActionResult> AddNewColor(string name, string hexCode)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(hexCode))
            {
                return Json(new { success = false, message = "يرجى إدخال اسم اللون ورمز الـ Hex" });
            }

            try
            {
                // إنشاء كائن اللون الجديد
                var newColor = new Color
                {
                    Name = name,
                    HexCode = hexCode
                };

                _context.Colors.Add(newColor);
                await _context.SaveChangesAsync();

                // نرجع بيانات اللون الجديد (Id, Name) عشان نضيفه للقائمة فوراً بالـ JS
                return Json(new { success = true, id = newColor.Id, name = newColor.Name, hex = newColor.HexCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }
        // GET: Admin/Products/Edit/5
        [HttpGet]       
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // التعديل هنا: إضافة Include لجلب البيانات المرتبطة
            var product = await _context.Products
                .Include(p => p.ProductCompositions)
                .Include(p => p.ProductTags)
                .Include(p => p.ProjectCategories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            // تجهيز كل الـ ViewBag المطلوبة لكي لا يظهر خطأ Null في الصفحة
            ViewBag.Categories = await _context.Categorise.Where(c => c.Status == CategoryStatus.Active).ToListAsync();
            ViewBag.Brands = await _context.Brands.Where(b => b.Status == "Active").ToListAsync();
            ViewBag.Compositions = await _context.Compositions.ToListAsync();
            ViewBag.ProductTags = await _context.ProductTags.ToListAsync();
            ViewBag.FabricTypes = await _context.FabricTypes.ToListAsync();
            ViewBag.Colors = await _context.Colors.ToListAsync();
            ViewBag.ProjectCategories = await _context.ProjectCategories.ToListAsync();

            return View(product);
        }

        // GET: Admin/Products/GetEdit/5
        [HttpGet]
        public async Task<IActionResult> GetEdit(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductTags) 
                .Include(p => p.ProductCompositions)
                .Include(p => p.ProjectCategories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            // Get dropdown data
            var categories = await _context.Categorise
                .Where(c => c.Status == CategoryStatus.Active)
                .ToListAsync();

            var brands = await _context.Brands
                .Where(b => b.Status == "Active")
                .ToListAsync();

            ViewBag.Categories = await _context.Categorise.Where(c => c.Status == CategoryStatus.Active).ToListAsync();
            ViewBag.Brands = await _context.Brands.Where(b => b.Status == "Active").ToListAsync();
            ViewBag.ProductTags = await _context.ProductTags.ToListAsync(); // هذا السطر اللي كان ناقص ومسبب الخطأ
            ViewBag.Compositions = await _context.Compositions.ToListAsync();
            ViewBag.Colors = await _context.Colors.ToListAsync();
            ViewBag.ProjectCategories = await _context.ProjectCategories.ToListAsync();
            return PartialView("_EditPartial", product);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]        
        public async Task<IActionResult> Edit(int id, Product product, int[] SelectedProjectCategoryIds, List<int> SelectedTagIds, List<ProductComposition> ProductCompositions)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. جلب المنتج الأصلي مع التضمينات (Includes)
                    var productDb = await _context.Products
                        .Include(p => p.ProductTags)
                        .Include(p => p.ProductCompositions)
                        .Include(p => p.ProjectCategories)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (productDb == null) return NotFound();

                    // 2. تحديث الحقول الأساسية (بما فيها ColorId تلقائياً)
                    _context.Entry(productDb).CurrentValues.SetValues(product);

                    productDb.UpdatedAt = DateTime.Now;
                    productDb.UpdatedBy = User.Identity?.Name ?? "System";

                    // 3. تحديث ProjectCategories (Many-to-Many)
                    productDb.ProjectCategories.Clear();
                    if (SelectedProjectCategoryIds != null)
                    {
                        var pCategories = await _context.ProjectCategories
                            .Where(pc => SelectedProjectCategoryIds.Contains(pc.Id)).ToListAsync();
                        foreach (var pc in pCategories) productDb.ProjectCategories.Add(pc);
                    }

                    // 4. تحديث الـ Tags
                    productDb.ProductTags.Clear();
                    if (SelectedTagIds != null)
                    {
                        var tags = await _context.ProductTags.Where(t => SelectedTagIds.Contains(t.Id)).ToListAsync();
                        foreach (var tag in tags) productDb.ProductTags.Add(tag);
                    }

                    // 5. تحديث الـ Compositions (حذف القديم وإضافة الجديد)
                    _context.ProductCompositions.RemoveRange(productDb.ProductCompositions);
                    if (ProductCompositions != null)
                    {
                        foreach (var item in ProductCompositions.Where(pc => pc.CompositionId > 0 && pc.Percentage > 0))
                        {
                            productDb.ProductCompositions.Add(new ProductComposition
                            {
                                ProductId = id,
                                CompositionId = item.CompositionId,
                                Percentage = item.Percentage
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث المنتج بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                }
            }

            // في حال فشل الـ Validation، نعيد تحميل القوائم لعرض الصفحة مرة أخرى
            await LoadViewBags();
            return View(product);
        }

        // ميثود مساعدة لتقليل تكرار الكود
        private async Task LoadViewBags()
        {
            ViewBag.Categories = await _context.Categorise.Where(c => c.Status == CategoryStatus.Active).ToListAsync();
            ViewBag.Brands = await _context.Brands.Where(b => b.Status == "Active").ToListAsync();
            ViewBag.Colors = await _context.Colors.ToListAsync();
            ViewBag.ProductTags = await _context.ProductTags.ToListAsync();
            ViewBag.FabricTypes = await _context.FabricTypes.ToListAsync();
            ViewBag.ProjectCategories = await _context.ProjectCategories.ToListAsync();
            ViewBag.Compositions = await _context.Compositions.ToListAsync();
        }

        // POST: Admin/Products/Delete/5
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found!" });
                }

                // Soft delete
                product.IsDeleted = true;
                product.DeletedAt = DateTime.Now;
                product.DeletedBy = User.Identity?.Name ?? "System";

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Product deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Admin/Products/DeleteMultiple
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "No products selected" });
                }

                var products = await _context.Products
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in products)
                {
                    product.IsDeleted = true;
                    product.DeletedAt = DateTime.Now;
                    product.DeletedBy = User.Identity?.Name ?? "System";
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"{products.Count} product(s) deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private string GenerateSlug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            return name.ToLower()
                .Trim()
                .Replace(" ", "-")
                .Replace("--", "-")
                .Replace("---", "-");
        }
    }
}