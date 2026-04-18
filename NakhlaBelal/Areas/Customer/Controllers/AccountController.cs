using Microsoft.AspNetCore.Mvc;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AccountController : Controller
    {
        // يعيد التوجيه لصفحة الملف الشخصي الحقيقية
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return Redirect("/Identity/Profile/Index");

            return Redirect("/Identity/Account/Login");
        }
    }
}
