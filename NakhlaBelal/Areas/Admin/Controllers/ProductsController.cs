using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Repositories.IRepositories;
using System.Linq;
using System.Threading.Tasks;

namespace NAKHLA.Controllers.Admin
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.SUPER_ADMIN_ROLE},{SD.ADMIN_ROLE},{SD.EMPLOYEE_ROLE}")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IProductRepository _productRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Color> _productColorRepository;

        public ProductsController(
            ApplicationDbContext context,
            IProductRepository productRepository,
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IRepository<Color> productColorRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productColorRepository = productColorRepository;
        }

        // ================= INDEX =================
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

            ViewBag.TotalProducts = await _context.Products.CountAsync(p => !p.IsDeleted);
            return View(products);
        }

        // ================= DETAILS =================
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

        // ================= CREATE =================
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

        [HttpPost]
        public async Task<IActionResult> Create(
    Product product,
    IFormFile img,
    List<IFormFile>? subImgs,
    int[] SelectedProjectCategoryIds,
    List<int> SelectedTagIds,
    List<ProductComposition> ProductCompositions)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ================= BASIC DATA =================
                    product.CreatedAt = DateTime.Now;
                    product.CreatedBy = User.Identity?.Name ?? "System";
                    product.IsDeleted = false;

                    if (string.IsNullOrEmpty(product.SKU))
                        product.SKU = "PROD-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();



                    // ================= MAIN IMAGE =================
                    if (img != null && img.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(img.FileName);

                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await img.CopyToAsync(stream);
                        }

                        product.MainImage = fileName;
                    }

                    // ================= RELATIONS =================
                    if (SelectedProjectCategoryIds != null)
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
                        product.ProductTags = _context.ProductTags
                            .Where(t => SelectedTagIds.Contains(t.Id))
                            .ToList();
                    }

                    if (ProductCompositions != null)
                    {
                        product.ProductCompositions = ProductCompositions
                            .Where(pc => pc.CompositionId > 0 && pc.Percentage > 0)
                            .ToList();
                    }

                    // ================= SAVE PRODUCT =================
                    await _productRepository.AddAsync(product);
                    await _productRepository.CommitAsync();

                    // ================= SUB IMAGES =================
                    if (subImgs != null && subImgs.Count > 0)
                    {
                        foreach (var file in subImgs)
                        {
                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/product_images", fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                // مهم: لازم يكون عندك DbSet ProductImages
                                _context.productImages.Add(new ProductImage
                                {
                                    ImageUrl = fileName,
                                    ProductId = product.Id
                                });
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            await LoadViewBags();
            return View(product);
        }

        // ================= EDIT GET =================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductCompositions)
                .Include(p => p.ProductTags)
                .Include(p => p.ProjectCategories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            await LoadViewBags();
            return View(product);
        }

        // ================= GET EDIT PARTIAL =================
        [HttpGet]
        public async Task<IActionResult> GetEdit(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductTags)
                .Include(p => p.ProductCompositions)
                .Include(p => p.ProjectCategories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return NotFound();

            await LoadViewBags();
            return PartialView("_EditPartial", product);
        }

        // ================= EDIT POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, int[] SelectedProjectCategoryIds, List<int> SelectedTagIds, List<ProductComposition> ProductCompositions)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var productDb = await _context.Products
                    .Include(p => p.ProductTags)
                    .Include(p => p.ProductCompositions)
                    .Include(p => p.ProjectCategories)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (productDb == null) return NotFound();

                _context.Entry(productDb).CurrentValues.SetValues(product);

                productDb.UpdatedAt = DateTime.Now;

                // ProjectCategories
                productDb.ProjectCategories.Clear();
                if (SelectedProjectCategoryIds != null)
                {
                    var cats = await _context.ProjectCategories
                        .Where(c => SelectedProjectCategoryIds.Contains(c.Id)).ToListAsync();
                    foreach (var c in cats) productDb.ProjectCategories.Add(c);
                }

                // Tags
                productDb.ProductTags.Clear();
                if (SelectedTagIds != null)
                {
                    var tags = await _context.ProductTags
                        .Where(t => SelectedTagIds.Contains(t.Id)).ToListAsync();
                    foreach (var t in tags) productDb.ProductTags.Add(t);
                }

                // Compositions
                _context.ProductCompositions.RemoveRange(productDb.ProductCompositions);
                if (ProductCompositions != null)
                {
                    foreach (var item in ProductCompositions.Where(x => x.CompositionId > 0 && x.Percentage > 0))
                    {
                        productDb.ProductCompositions.Add(new ProductComposition
                        {
                            ProductId = id,
                            CompositionId = item.CompositionId,
                            Percentage = item.Percentage
                        });
                    }
                }

                await _productRepository.CommitAsync();

                return RedirectToAction(nameof(Index));
            }

            await LoadViewBags();
            return View(product);
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.FindAsync(id);
            if (product == null) return Json(new { success = false });

            product.IsDeleted = true;
            product.DeletedAt = DateTime.Now;

            await _productRepository.UpdateAsync(product);
            await _productRepository.CommitAsync();

            return Json(new { success = true });
        }

        // ================= HELPERS =================
        private async Task LoadViewBags()
        {
            ViewBag.Categories = await _categoryRepository.GetAsync(c => c.Status == CategoryStatus.Active);
            ViewBag.Brands = await _brandRepository.GetAsync(b => b.Status == "Active");

            ViewBag.Colors = await _context.Colors.ToListAsync();
            ViewBag.ProductTags = await _context.ProductTags.ToListAsync();
            ViewBag.FabricTypes = await _context.FabricTypes.ToListAsync();
            ViewBag.ProjectCategories = await _context.ProjectCategories.ToListAsync();
            ViewBag.Compositions = await _context.Compositions.ToListAsync();
        }
    }
}