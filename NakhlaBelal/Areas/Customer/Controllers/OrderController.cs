using Microsoft.AspNetCore.Mvc;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        // يعيد التوجيه لصفحة التتبّع الموجودة مسبقاً
        public IActionResult Track() => RedirectToAction("Index", "Tracking");

        public IActionResult RequestSample() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestSample(string fullName, string email, string phone, string address, string fabrics)
        {
            TempData["success-notification"] = "تم استلام طلب العيّنات! سنتواصل معك لتأكيد الإرسال.";
            return RedirectToAction(nameof(RequestSample));
        }
    }
}
