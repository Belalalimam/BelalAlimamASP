using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    /// <summary>
    /// تصنيفات ثيمية للأقمشة — كل Action يعرض منتجات مصفّاة حسب الموضوع
    /// </summary>
    [Area("Customer")]
    public class FabricsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public FabricsController(ApplicationDbContext context) { _context = context; }

        private async Task<IActionResult> ShowCollection(string titleKey, string subtitle, string icon, Func<IQueryable<Product>, IQueryable<Product>> filter)
        {
            var baseQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => !p.IsDeleted);

            var products = await filter(baseQuery)
                .OrderByDescending(p => p.CreatedAt)
                .Take(24)
                .ToListAsync();

            ViewBag.PageTitle = titleKey;
            ViewBag.PageSubtitle = subtitle;
            ViewBag.PageIcon = icon;
            return View("Collection", products);
        }

        public Task<IActionResult> NewArrivals() =>
            ShowCollection("وصل حديثاً", "أحدث الأقمشة التي انضمت لمجموعتنا", "🆕",
                q => q.Where(p => p.CreatedAt >= DateTime.Now.AddDays(-30)));

        public Task<IActionResult> OnSale() =>
            ShowCollection("الأقمشة المخفّضة", "وفّر على مفضّلاتك الآن", "🔥",
                q => q.Where(p => p.Discount > 0));

        public Task<IActionResult> Bridal() =>
            ShowCollection("أقمشة الأعراس", "فخامة ولمسات لا تُنسى ليوم العمر", "👰",
                q => q.Where(p => p.Name.Contains("عرس") || p.Name.Contains("زفاف") || (p.Category != null && p.Category.Name.Contains("عرس"))));

        public Task<IActionResult> SpecialOccasions() =>
            ShowCollection("أقمشة المناسبات الخاصة", "أناقة لكل مناسبة مميّزة", "✨",
                q => q.Where(p => p.Name.Contains("مناسبات") || p.Name.Contains("سهرة")));

        public Task<IActionResult> Coating() =>
            ShowCollection("أقمشة المعاطف", "دفء وأناقة في كل تفصيلة", "🧥",
                q => q.Where(p => p.Name.Contains("معطف") || p.Name.Contains("جاكيت")));

        public Task<IActionResult> Suiting() =>
            ShowCollection("أقمشة البدل الراقية", "للرجل العصري الأنيق", "👔",
                q => q.Where(p => p.Name.Contains("بدلة") || p.Name.Contains("suit")));

        public Task<IActionResult> Prints() =>
            ShowCollection("الأقمشة المطبوعة", "تصاميم ونقوش تعكس ذوقك", "🎨",
                q => q.Where(p => p.Name.Contains("مطبوع") || p.Name.Contains("نقش") || p.Name.Contains("print")));

        public Task<IActionResult> Jacquards() =>
            ShowCollection("أقمشة الجاكار", "نسيج فاخر بتفاصيل دقيقة", "🧵",
                q => q.Where(p => p.Name.Contains("جاكار") || p.Name.Contains("jacquard")));

        public Task<IActionResult> Laces() =>
            ShowCollection("الدانتيل والتطريز", "رومانسية وأناقة في كل خيط", "🌸",
                q => q.Where(p => p.Name.Contains("دانتيل") || p.Name.Contains("تطريز") || p.Name.Contains("lace")));

        public Task<IActionResult> Boucle() =>
            ShowCollection("أقمشة البوكليه", "نسيج ناعم وراقٍ للملابس الخريفية", "🧶",
                q => q.Where(p => p.Name.Contains("بوكليه") || p.Name.Contains("boucle")));

        public Task<IActionResult> Velvet() =>
            ShowCollection("القطيفة (فيلفيت)", "ملمس فاخر ولون غني", "💎",
                q => q.Where(p => p.Name.Contains("قطيفة") || p.Name.Contains("velvet")));
    }
}
