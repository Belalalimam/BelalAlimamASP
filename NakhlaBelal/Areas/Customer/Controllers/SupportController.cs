using Microsoft.AspNetCore.Mvc;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class SupportController : Controller
    {
        public IActionResult Contact() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string orderNumber, string message)
        {
            TempData["success-notification"] = "تم إرسال رسالتك بنجاح! فريق الدعم سيتواصل معك قريباً.";
            return RedirectToAction(nameof(Contact));
        }
    }
}
