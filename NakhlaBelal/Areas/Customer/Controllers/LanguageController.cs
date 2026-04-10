using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class LanguageController : Controller
    {
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            // حفظ اللغة في الـ Cookies لمدة سنة
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            // العودة للصفحة السابقة
            return LocalRedirect(returnUrl);
        }
    }
}
