using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Controllers.Api;

public class BrandsController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public BrandsController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var brands = await _context.Brands
            .Where(b => b.Status == "Active")
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name, b.Website, b.IsFeatured })
            .ToListAsync();

        return ApiOk(brands);
    }
}
