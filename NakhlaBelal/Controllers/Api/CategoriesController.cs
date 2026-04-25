using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Controllers.Api;

public class CategoriesController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categorise
            .Where(c => c.Status == Models.CategoryStatus.Active)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new { c.Id, c.Name, c.Slug, c.ImageUrl, c.IconClass, c.Description })
            .ToListAsync();

        return ApiOk(categories);
    }
}
