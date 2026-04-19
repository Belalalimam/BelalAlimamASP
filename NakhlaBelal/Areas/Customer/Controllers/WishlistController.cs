using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var uid = _userManager.GetUserId(User);
            var items = await _context.Wishlists
                .Include(w => w.Product).ThenInclude(p => p!.Category)
                .Where(w => w.ApplicationUserId == uid)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var uid = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(uid))
                return Json(new { ok = false, requiresAuth = true });

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.ApplicationUserId == uid && w.ProductId == productId);

            bool added;
            if (existing == null)
            {
                _context.Wishlists.Add(new Wishlist { ApplicationUserId = uid, ProductId = productId });
                added = true;
            }
            else
            {
                _context.Wishlists.Remove(existing);
                added = false;
            }

            await _context.SaveChangesAsync();
            var count = await _context.Wishlists.CountAsync(w => w.ApplicationUserId == uid);
            return Json(new { ok = true, added, count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var uid = _userManager.GetUserId(User);
            var item = await _context.Wishlists.FirstOrDefaultAsync(w => w.Id == id && w.ApplicationUserId == uid);
            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var uid = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(uid)) return Json(new { count = 0 });
            var count = await _context.Wishlists.CountAsync(w => w.ApplicationUserId == uid);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> MyIds()
        {
            var uid = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(uid)) return Json(new int[0]);
            var ids = await _context.Wishlists
                .Where(w => w.ApplicationUserId == uid)
                .Select(w => w.ProductId)
                .ToListAsync();
            return Json(ids);
        }
    }
}
