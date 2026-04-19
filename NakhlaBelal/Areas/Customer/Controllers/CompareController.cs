using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Accepts ?ids=1,2,3 (up to 4) and renders comparison view
        public async Task<IActionResult> Index(string? ids)
        {
            var list = new List<int>();
            if (!string.IsNullOrWhiteSpace(ids))
            {
                foreach (var s in ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(s.Trim(), out var n)) list.Add(n);
                }
            }
            list = list.Distinct().Take(4).ToList();

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.FabricType)
                .Include(p => p.Color)
                .Include(p => p.ProductCompositions).ThenInclude(pc => pc.Composition)
                .Where(p => list.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync();

            // Keep requested order
            products = list.Select(id => products.FirstOrDefault(p => p.Id == id))
                           .Where(p => p != null)
                           .ToList()!;

            return View(products);
        }
    }
}
