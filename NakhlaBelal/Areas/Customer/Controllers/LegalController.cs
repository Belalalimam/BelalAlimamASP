using Microsoft.AspNetCore.Mvc;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class LegalController : Controller
    {
        public IActionResult Terms() => View();
        public IActionResult Privacy() => View();
        public IActionResult Cookies() => View();
        public IActionResult ODR() => View();
        public IActionResult Labelling() => View();
        public IActionResult Accessibility() => View();
    }
}
