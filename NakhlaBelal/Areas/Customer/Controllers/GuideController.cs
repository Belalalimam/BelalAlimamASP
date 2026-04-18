using Microsoft.AspNetCore.Mvc;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class GuideController : Controller
    {
        public IActionResult Glossary() => View();
        public IActionResult Weaves() => View();
        public IActionResult Drape() => View();
        public IActionResult Abbreviations() => View();
    }
}
