using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.ViewModels;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.ProjectCategories = _context.ProjectCategories.ToList();
            ViewBag.FabricTypes = _context.FabricTypes.ToList();
            ViewBag.Colors = _context.Colors.ToList(); // افترضنا أن اسم الجدول Colors
            return View();
        }
        public IActionResult Product(FilterVM filterVM, int page = 1)
        {
            // جلب المنتجات مع كل العلاقات اللازمة
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.FabricType)
                .Include(p => p.Color)
                .Include(p => p.ProjectCategories)
                .Include(p => p.ProductCompositions)
        .ThenInclude(pc => pc.Composition)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            // تطبيق الفلاتر "بذكاء":
            if (!string.IsNullOrEmpty(filterVM.Name))
                products = products.Where(p => p.Name.Contains(filterVM.Name));

            if (filterVM.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == filterVM.CategoryId);

            if (filterVM.FabricTypeId.HasValue)
                products = products.Where(p => p.FabricTypeId == filterVM.FabricTypeId);

            if (filterVM.ColorId.HasValue)
                products = products.Where(p => p.ColorId == filterVM.ColorId);

            // فلترة المشاريع (العلاقة Many-to-Many)
            if (filterVM.ProjectCategoryId.HasValue)
                products = products.Where(p => p.ProjectCategories.Any(c => c.Id == filterVM.ProjectCategoryId));

            // فلترة المكونات (العلاقة عبر جدول وسيط)
            if (filterVM.CompositionId.HasValue)
                products = products.Where(p => p.ProductCompositions.Any(pc => pc.CompositionId == filterVM.CompositionId));

            // الترقيم (Pagination)
            const int pageSize = 12;
            var totalItems = products.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var result = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Filter = filterVM; // عشان الـ View تعرف شو الفلتر الحالي

            return View(result);
        }

        public IActionResult CategroySearch(FilterVM filterVM, int page = 1)
        {
            // جلب المنتجات مع كل العلاقات اللازمة
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.FabricType)
                .Include(p => p.Color)
                .Include(p => p.ProjectCategories)
                .Include(p => p.ProductCompositions)
                .ThenInclude(pc => pc.Composition)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            // تطبيق الفلاتر "بذكاء":
            if (!string.IsNullOrEmpty(filterVM.Name))
                products = products.Where(p => p.Name.Contains(filterVM.Name));

            if (filterVM.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == filterVM.CategoryId);


            FabricType? fabricType = null;
            if (filterVM.FabricTypeId.HasValue)
            {
                products = products.Where(p => p.FabricTypeId == filterVM.FabricTypeId);
                fabricType = _context.FabricTypes.FirstOrDefault(e => e.Id == filterVM.FabricTypeId);
                ViewBag.FabricType = filterVM.FabricTypeId;
            }
            ViewBag.SelectedFabricType = fabricType;




            // فلترة المشاريع (العلاقة Many-to-Many)
            ProjectCategory? projectCategory = null;
            if (filterVM.ProjectCategoryId.HasValue)
            {
                products = products.Where(p => p.ProjectCategories.Any(c => c.Id == filterVM.ProjectCategoryId));
                projectCategory = _context.ProjectCategories.FirstOrDefault(e => e.Id == filterVM.ProjectCategoryId);
                ViewBag.ProjectCategory = filterVM.ProjectCategoryId;
            }

            ViewBag.SelectedProject = projectCategory;



            Color? color = null;
            if (filterVM.ColorId.HasValue)
            {
                products = products.Where(p => p.ColorId == filterVM.ColorId);
                color = _context.Colors.FirstOrDefault(e => e.Id == filterVM.ColorId);
                ViewBag.Color = filterVM.ColorId;
            }

            ViewBag.SelectedColor = color;



            // فلترة المكونات (العلاقة عبر جدول وسيط)
            Composition composition = null;
            if (filterVM.CompositionId.HasValue) { 
                products = products.Where(p => p.ProductCompositions.Any(pc => pc.CompositionId == filterVM.CompositionId));
                composition = _context.Compositions.FirstOrDefault(e => e.Id == filterVM.CompositionId);
            ViewBag.ProductComposition = filterVM.CompositionId;
        }

        ViewBag.SelectedComposition = composition;



            // الترقيم (Pagination)
            const int pageSize = 12;
            var totalItems = products.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var result = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Filter = filterVM; // عشان الـ View تعرف شو الفلتر الحالي





            Category? category = null;

            if (filterVM.CategoryId is not null)
            {
                products = products.Where(e => e.CategoryId == filterVM.CategoryId);
                category = _context.Categorise
                    .FirstOrDefault(e => e.Id == filterVM.CategoryId);

                ViewBag.CategoryId = filterVM.CategoryId;
            }

            ViewBag.SelectedCategory = category;

            return View(result);
        }


        [HttpGet("fabrics-used-for/{slug}")]
        public IActionResult FabricsUsedFor(string slug)
        {
            var products = _context.Products
                .Include(p => p.ProjectCategories)
                .Include(p => p.FabricType)
                .Where(p => p.ProjectCategories.Any(c => c.Slug == slug))
                .ToList();

            ViewBag.Title = slug;
            return View("Product", products);
        }


        [HttpGet("fabric-type/{slug}")]
        public IActionResult FabricType(string slug)
        {
            var products = _context.Products
                .Include(p => p.FabricType)
                .Where(p => p.FabricType.Slug == slug)
                .ToList();

            ViewBag.Title = slug;
            return View("Product", products);
        }




        public IActionResult FilterByColor(string color)
        {
            var products = _context.Products
                .Include(p => p.Color)
                .ToList();

            ViewBag.categories = _context.Categorise.ToList();
            ViewBag.Title = $"Color: {color}";
            return View("Product", products);
        }




        public IActionResult Details(int id)
        {
            // 1. جلب بيانات المنتج مع كافة العلاقات الضرورية لمنع خطأ الـ Null أو الـ InvalidOperation
            var product = _context.Products
        .Include(e => e.Category)
        .Include(p => p.ProjectCategories)
        .Include(e => e.ProductImages)
        .Include(p => p.Color) // تم حذف ThenInclude للون لأنه string وليس كائن
        .Include(p => p.ProductCompositions)
            .ThenInclude(pc => pc.Composition) // هذا يبقى كما هو لأن Composition كائن
        .FirstOrDefault(e => e.Id == id && !e.IsDeleted);

            // التحقق من وجود المنتج
            if (product is null)
                return RedirectToAction(nameof(NotFoundPage));

            // تحديث عدد المشاهدات
            product.Traffic += 1;
            _context.SaveChanges();

            // 2. جلب المنتجات ذات الصلة (نفس القسم)
            var relatedProducts = _context.Products
                .Include(e => e.Category)
                .Where(e => e.CategoryId == product.CategoryId && e.Name == product.Name && e.Id != product.Id && !e.IsDeleted)
                .Take(4).ToList();

            // 3. جلب المنتجات الأكثر رواجاً
            var topProducts = _context.Products
                .Include(e => e.Category)
                .Where(e => e.Id != product.Id && !e.IsDeleted)
                .OrderByDescending(e => e.Traffic).Take(4).ToList();

            // 4. جلب المنتجات المتشابهة بالاسم
            var similarProducts = _context.Products
                .Include(e => e.Category)
                .Where(e => e.Name.Contains(product.Name) && e.Id != product.Id)
                .Take(4)
                .ToList();

            // 5. تجهيز الصور
            var productImages = product.ProductImages?
                .OrderBy(pi => pi.DisplayOrder)
                .ToList() ?? new List<ProductImage>();

            // 6. تجهيز الـ ViewModel مع حل مشكلة تكرار المكونات
            var viewModel = new ProductWithRelatedVM()
            {
                Product = product,
                RelatedProducts = relatedProducts,
                TopProducts = topProducts,
                SimilarProducts = similarProducts,
                ProductImages = productImages,

                // الحل السحري للتكرار: GroupBy تضمن ظهور كل مكون مرة واحدة فقط
                SelectedCompositions = product.ProductCompositions
                    .Where(pc => pc.Composition != null) // تأمين إضافي ضد الـ Null
                    .GroupBy(pc => pc.Composition.Name)
                    .Select(g => new ProductCompositionVM
                    {
                        CompositionName = g.Key,
                        Percentage = g.First().Percentage // يأخذ النسبة من أول سجل في المجموعة
                    }).ToList()
            };

            // إرسال الكاتيجوري للـ View لضمان عدم حدوث خطأ في القائمة الجانبية
            ViewBag.categories = _context.Categorise.ToList();

            return View(viewModel);
        }


        public IActionResult NotFoundPage()
        {
            return View();
        }



        [HttpGet("filter-by-composition/{id}")]
        public IActionResult FilterByComposition(int id)
        {
            var products = _context.Products
                .Include(p => p.ProductCompositions)
                .Where(p => p.ProductCompositions.Any(pc => pc.CompositionId == id))
                .ToList();

            var compName = _context.Compositions.Find(id)?.Name;
            ViewBag.Title = $"Fabrics with {compName}";

            return View("Product", products);
        }


        public ViewResult PersonalInfo()
        {
            string name = "Mohamed";
            int age = 27;
            string address = "Mansoura";
            char gender = 'M';

            List<string> skills = new List<string>
        {
            "C", "C++", "C#", "SQL Server"
        };

            var PersonalInfoVM = new PersonalInfoVM()
            {
                Name = name,
                Age = age,
                Address = address,
                Gender = gender,
                Skills = skills
            };

            return View("PersonalInfomation", PersonalInfoVM);
        }

        public ViewResult PersonalInfo2()
        {
            string name = "Mohamed";
            int age = 27;
            string address = "Mansoura";
            char gender = 'M';

            List<string> skills = new List<string>
        {
            "C", "C++", "C#", "SQL Server"
        };

            var PersonalInfoVM = new PersonalInfoVM()
            {
                Name = name,
                Age = age,
                Address = address,
                Gender = gender,
                Skills = skills
            };

            return View(PersonalInfoVM);
        }
    }
}