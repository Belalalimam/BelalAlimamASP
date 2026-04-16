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

            var totalProducts = await _context.Products.CountAsync(p => !p.IsDeleted);
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            ViewBag.TotalProducts = totalProducts;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

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

        // ================= GET DETAILS PARTIAL =================
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

        // ================= CREATE GET =================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadViewBags();
            return View();
        }

        // ================= CREATE POST =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IFormFile img,
            List<IFormFile>? galleryImages,
            int[] SelectedProjectCategoryIds,
            List<int> SelectedTagIds,
            List<ProductComposition> ProductCompositions)
        {
            ModelState.Remove("MainImage");
            if (ModelState.IsValid)
            {
                try
                {
                    // ===== BASIC DATA =====
                    product.CreatedAt = DateTime.Now;
                    product.CreatedBy = User.Identity?.Name ?? "System";
                    product.IsDeleted = false;

                    if (string.IsNullOrEmpty(product.SKU))
                        product.SKU = "PROD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                    // ===== MAIN IMAGE =====
                    if (img != null && img.Length > 0)
                    {
                        var mainImgDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                        Directory.CreateDirectory(mainImgDir); // ينشئ المجلد إذا ما موجود

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(img.FileName);
                        var filePath = Path.Combine(mainImgDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await img.CopyToAsync(stream);

                        product.MainImage = fileName;
                    }

                    // ===== GALLERY IMAGES - احفظ مسارات الصور قبل حفظ المنتج =====
                    var galleryFileNames = new List<string>();

                    if (galleryImages != null && galleryImages.Count > 0)
                    {
                        var galleryDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "product_images");
                        Directory.CreateDirectory(galleryDir); // ينشئ المجلد إذا ما موجود

                        foreach (var file in galleryImages)
                        {
                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                var filePath = Path.Combine(galleryDir, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                    await file.CopyToAsync(stream);

                                galleryFileNames.Add(fileName);
                            }
                        }

                        // احفظ JSON قبل إنشاء المنتج
                        product.GalleryImages = galleryFileNames;
                    }

                    // ===== RELATIONS =====
                    if (SelectedProjectCategoryIds != null && SelectedProjectCategoryIds.Length > 0)
                    {
                        product.ProjectCategories = new List<ProjectCategory>();
                        foreach (var catId in SelectedProjectCategoryIds)
                        {
                            var category = await _context.ProjectCategories.FindAsync(catId);
                            if (category != null)
                                product.ProjectCategories.Add(category);
                        }
                    }

                    if (SelectedTagIds != null && SelectedTagIds.Any())
                    {
                        product.ProductTags = await _context.ProductTags
                            .Where(t => SelectedTagIds.Contains(t.Id))
                            .ToListAsync();
                    }

                    if (ProductCompositions != null)
                    {
                        product.ProductCompositions = ProductCompositions
                            .Where(pc => pc.CompositionId > 0 && pc.Percentage > 0)
                            .ToList();
                    }

                    // ===== SAVE PRODUCT =====
                    await _productRepository.AddAsync(product);
                    await _productRepository.CommitAsync();

                    // ===== SAVE PRODUCT IMAGES TABLE =====
                    if (galleryFileNames.Any())
                    {
                        int order = 1;
                        foreach (var fileName in galleryFileNames)
                        {
                            _context.productImages.Add(new ProductImage
                            {
                                ImageUrl = fileName,
                                ProductId = product.Id,
                                DisplayOrder = order++,
                                AltText = product.Name
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "تم إنشاء المنتج بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ: " + ex.Message);
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
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IFormFile? img,
            List<IFormFile>? galleryImages,
            int[] SelectedProjectCategoryIds,
            List<int> SelectedTagIds,
            List<ProductComposition> ProductCompositions)
        {
            if (id != product.Id) return NotFound();
            ModelState.Remove("MainImage");
            if (ModelState.IsValid)
            {
                try
                {
                    var productDb = await _context.Products
                        .Include(p => p.ProductTags)
                        .Include(p => p.ProductCompositions)
                        .Include(p => p.ProjectCategories)
                        .FirstOrDefaultAsync(p => p.Id == id);

                    if (productDb == null) return NotFound();

                    // ===== MAIN IMAGE =====
                    if (img != null && img.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(productDb.MainImage))
                        {
                            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", productDb.MainImage);
                            if (System.IO.File.Exists(oldPath))
                                System.IO.File.Delete(oldPath);
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(img.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await img.CopyToAsync(stream);

                        product.MainImage = fileName;
                    }
                    else
                    {
                        product.MainImage = productDb.MainImage;
                    }

                    // ===== UPDATE VALUES =====
                    _context.Entry(productDb).CurrentValues.SetValues(product);
                    productDb.UpdatedAt = DateTime.Now;
                    productDb.UpdatedBy = User.Identity?.Name ?? "System";

                    // ===== RELATIONS =====
                    productDb.ProjectCategories.Clear();
                    if (SelectedProjectCategoryIds != null && SelectedProjectCategoryIds.Length > 0)
                    {
                        var cats = await _context.ProjectCategories
                            .Where(c => SelectedProjectCategoryIds.Contains(c.Id))
                            .ToListAsync();
                        foreach (var c in cats)
                            productDb.ProjectCategories.Add(c);
                    }

                    productDb.ProductTags.Clear();
                    if (SelectedTagIds != null && SelectedTagIds.Any())
                    {
                        var tags = await _context.ProductTags
                            .Where(t => SelectedTagIds.Contains(t.Id))
                            .ToListAsync();
                        foreach (var t in tags)
                            productDb.ProductTags.Add(t);
                    }

                    // ===== COMPOSITIONS =====
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

                    // ===== GALLERY IMAGES =====
                    if (galleryImages != null && galleryImages.Count > 0)
                    {
                        var newGalleryPaths = new List<string>();

                        foreach (var file in galleryImages)
                        {
                            if (file.Length > 0)
                            {
                                var subFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                var subFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/product_images", subFileName);

                                using (var stream = new FileStream(subFilePath, FileMode.Create))
                                    await file.CopyToAsync(stream);

                                _context.productImages.Add(new ProductImage
                                {
                                    ImageUrl = subFileName,
                                    ProductId = id
                                });

                                newGalleryPaths.Add(subFileName);
                            }
                        }

                        var existingGallery = productDb.GalleryImages;
                        existingGallery.AddRange(newGalleryPaths);
                        productDb.GalleryImages = existingGallery;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث المنتج بنجاح";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء التعديل: " + ex.Message);
                }
            }
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            await LoadViewBags();
            return Json(new { success = false, error = string.Join(", ", errors) });
        }

        // ================= DELETE =================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "المنتج غير موجود" });

            product.IsDeleted = true;
            product.DeletedAt = DateTime.Now;
            product.DeletedBy = User.Identity?.Name ?? "System";

            await _productRepository.UpdateAsync(product);
            await _productRepository.CommitAsync();

            return Json(new { success = true });
        }

        // ================= TOGGLE STATUS =================
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await _productRepository.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "المنتج غير موجود" });

            product.Status = !product.Status;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = User.Identity?.Name ?? "System";

            await _productRepository.UpdateAsync(product);
            await _productRepository.CommitAsync();

            return Json(new { success = true, isActive = product.Status });
        }

        // ================= TOGGLE FEATURED =================
        [HttpPost]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var product = await _productRepository.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "المنتج غير موجود" });

            product.IsFeatured = !product.IsFeatured;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = User.Identity?.Name ?? "System";

            await _productRepository.UpdateAsync(product);
            await _productRepository.CommitAsync();

            return Json(new { success = true, isFeatured = product.IsFeatured });
        }

        // ================= TOGGLE BEST SELLER =================
        [HttpPost]
        public async Task<IActionResult> ToggleBestSeller(int id)
        {
            var product = await _productRepository.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "المنتج غير موجود" });

            product.IsBestSeller = !product.IsBestSeller;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = User.Identity?.Name ?? "System";

            await _productRepository.UpdateAsync(product);
            await _productRepository.CommitAsync();

            return Json(new { success = true, isBestSeller = product.IsBestSeller });
        }

        // ================= UPDATE STOCK =================
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int id, int quantity)
        {
            var product = await _productRepository.FindAsync(id);
            if (product == null) return Json(new { success = false, message = "المنتج غير موجود" });

            product.StockQuantity = quantity;
            product.IsInStock = quantity > 0;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = User.Identity?.Name ?? "System";

            await _productRepository.UpdateAsync(product);
            await _productRepository.CommitAsync();

            return Json(new
            {
                success = true,
                quantity = product.StockQuantity,
                isInStock = product.IsInStock,
                isLowStock = product.IsLowStock
            });
        }

        // ================= DELETE GALLERY IMAGE =================
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int productId, int imageId)
        {
            var image = await _context.productImages
                .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId);

            if (image == null) return Json(new { success = false, message = "الصورة غير موجودة" });

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/product_images", image.ImageUrl);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.productImages.Remove(image);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ================= BULK DELETE =================
        [HttpPost]
        public async Task<IActionResult> BulkDelete(int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return Json(new { success = false, message = "لم يتم تحديد منتجات" });

            var products = await _context.Products
                .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsDeleted = true;
                product.DeletedAt = DateTime.Now;
                product.DeletedBy = User.Identity?.Name ?? "System";
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, count = products.Count });
        }

        // ================= RESTORE =================
        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted);

            if (product == null) return Json(new { success = false, message = "المنتج غير موجود" });

            product.IsDeleted = false;
            product.DeletedAt = null;
            product.DeletedBy = null;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ================= DELETED PRODUCTS =================
        [HttpGet]
        public async Task<IActionResult> Deleted()
        {
            var deletedProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsDeleted)
                .OrderByDescending(p => p.DeletedAt)
                .ToListAsync();

            return View(deletedProducts);
        }

        // ================= SEARCH =================
        [HttpGet]
        public async Task<IActionResult> Search(string term, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { success = false, message = "أدخل كلمة بحث" });

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => !p.IsDeleted &&
                            (p.Name.Contains(term) ||
                             p.SKU.Contains(term) ||
                             (p.Barcode != null && p.Barcode.Contains(term))));

            var total = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.Price,
                    p.StockQuantity,
                    p.Status,
                    p.IsFeatured,
                    p.MainImage,
                    CategoryName = p.Category != null ? p.Category.Name : "",
                    BrandName = p.Brand != null ? p.Brand.Name : ""
                })
                .ToListAsync();

            return Json(new { success = true, data = products, total });
        }

        // ================= FILTER =================
        [HttpGet]
        public async Task<IActionResult> Filter(
            int? categoryId,
            int? brandId,
            bool? status,
            bool? isFeatured,
            bool? inStock,
            decimal? minPrice,
            decimal? maxPrice,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => !p.IsDeleted);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (isFeatured.HasValue)
                query = query.Where(p => p.IsFeatured == isFeatured.Value);

            if (inStock.HasValue && inStock.Value)
                query = query.Where(p => p.StockQuantity > 0);
            else if (inStock.HasValue && !inStock.Value)
                query = query.Where(p => p.StockQuantity == 0);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = total;

            return View("Index", products);
        }

        // ================= DUPLICATE =================
        [HttpPost]
        public async Task<IActionResult> Duplicate(int id)
        {
            var original = await _context.Products
                .Include(p => p.ProductCompositions)
                .Include(p => p.ProductTags)
                .Include(p => p.ProjectCategories)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (original == null) return Json(new { success = false, message = "المنتج غير موجود" });

            var copy = new Product
            {
                Name = original.Name + " (نسخة)",
                ShortDescription = original.ShortDescription,
                Description = original.Description,
                SKU = "PROD-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                Barcode = null,
                CostPrice = original.CostPrice,
                Price = original.Price,
                UnitPrice = original.UnitPrice,
                Discount = original.Discount,
                StockQuantity = 0,
                LowStockThreshold = original.LowStockThreshold,
                ManageStock = original.ManageStock,
                IsInStock = false,
                CategoryId = original.CategoryId,
                BrandId = original.BrandId,
                ColorId = original.ColorId,
                FabricTypeId = original.FabricTypeId,
                Weight = original.Weight,
                Width = original.Width,
                Height = original.Height,
                MainImage = original.MainImage,
                VideoUrl = original.VideoUrl,
                Slug = original.Slug + "-copy-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                MetaTitle = original.MetaTitle,
                MetaDescription = original.MetaDescription,
                MetaKeywords = original.MetaKeywords,
                Status = false,
                IsFeatured = false,
                IsNew = true,
                IsBestSeller = false,
                RequiresShipping = original.RequiresShipping,
                Taxable = original.Taxable,
                TaxClass = original.TaxClass,
                Specifications = original.Specifications,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "System",
                IsDeleted = false
            };

            if (original.ProductCompositions != null)
            {
                copy.ProductCompositions = original.ProductCompositions
                    .Select(pc => new ProductComposition
                    {
                        CompositionId = pc.CompositionId,
                        Percentage = pc.Percentage
                    }).ToList();
            }

            copy.ProductTags = original.ProductTags?.ToList() ?? new List<ProductTag>();
            copy.ProjectCategories = original.ProjectCategories?.ToList() ?? new List<ProjectCategory>();

            await _productRepository.AddAsync(copy);
            await _productRepository.CommitAsync();

            return Json(new { success = true, newId = copy.Id, message = "تم نسخ المنتج بنجاح" });
        }

        // ================= ADD NEW COLOR =================
        [HttpPost]
        public async Task<IActionResult> AddNewColor(string name, string hexCode)
        {
            if (string.IsNullOrEmpty(name))
                return Json(new { success = false, message = "الاسم مطلوب" });

            var color = new Color { Name = name, HexCode = hexCode };
            _context.Colors.Add(color);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = color.Id, name = color.Name });
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