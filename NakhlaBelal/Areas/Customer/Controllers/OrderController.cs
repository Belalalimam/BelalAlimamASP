using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext? _context;

        public OrderController(ApplicationDbContext? context = null)
        {
            _context = context;
        }

        // يعيد التوجيه لصفحة التتبّع الموجودة مسبقاً
        public IActionResult Track() => RedirectToAction("Index", "Tracking");

        public IActionResult RequestSample(int? productId)
        {
            if (productId.HasValue && _context != null)
            {
                var p = _context.Products
                    .Where(x => x.Id == productId.Value && !x.IsDeleted)
                    .Select(x => new { x.Id, x.Name, x.SKU })
                    .FirstOrDefault();
                if (p != null)
                {
                    ViewBag.PreselectedProductId = p.Id;
                    ViewBag.PreselectedProductName = p.Name;
                    ViewBag.PreselectedProductSKU = p.SKU;
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestSample(string fullName, string email, string phone, string address, string fabrics, int? productId = null)
        {
            // productId is optional — if present, append it to fabrics line for the ops team
            if (productId.HasValue)
            {
                fabrics = $"[Product #{productId}] {fabrics}";
            }
            TempData["success-notification"] = "تم استلام طلب العيّنات! سنتواصل معك لتأكيد الإرسال.";
            return RedirectToAction(nameof(RequestSample));
        }

        // Ajax one-click sample request from a product card / quick view
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickSample(int productId)
        {
            // Stub: in a real flow we'd persist a SampleRequest row with the user id
            return Json(new { ok = true, message = "تم تسجيل طلب العيّنة — أكمل تفاصيل التوصيل لاحقاً." });
        }
    }
}
