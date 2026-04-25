using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Controllers.Api;

public class ColorsController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public ColorsController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var colors = await _context.Colors
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.HexCode })
            .ToListAsync();

        return ApiOk(colors);
    }
}
