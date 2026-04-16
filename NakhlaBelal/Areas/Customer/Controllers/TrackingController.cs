using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Models;

namespace NAKHLA.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class TrackingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrackingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Customer/Tracking
        [HttpGet]
        public IActionResult Index(string? orderNumber = null)
        {
            ViewBag.PrefilledOrderNumber = orderNumber;
            return View();
        }

        // POST: /Customer/Tracking/Track
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(string orderNumber, string email)
        {
            if (string.IsNullOrWhiteSpace(orderNumber) || string.IsNullOrWhiteSpace(email))
            {
                TempData["error-notification"] = "يرجى إدخال رقم الطلب والبريد الإلكتروني";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o =>
                    o.OrderNumber == orderNumber.Trim() &&
                    o.ShippingEmail == email.Trim() &&
                    !o.IsDeleted);

            if (order == null)
            {
                TempData["error-notification"] = "لم يتم العثور على طلب بهذه البيانات. تأكد من رقم الطلب والبريد الإلكتروني.";
                ViewBag.PrefilledOrderNumber = orderNumber;
                return View(nameof(Index));
            }

            return View("Result", order);
        }
    }
}
