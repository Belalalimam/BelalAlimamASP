using Microsoft.AspNetCore.Mvc;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HelpController : Controller
    {
        public IActionResult FAQs() => View();
        public IActionResult Contact() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            // في الواقع نحفظها أو نبعث إيميل — الآن نكتفي بـ TempData
            TempData["success-notification"] = "شكراً لتواصلك! سنرد عليك في أقرب وقت.";
            return RedirectToAction(nameof(Contact));
        }
    }
}
