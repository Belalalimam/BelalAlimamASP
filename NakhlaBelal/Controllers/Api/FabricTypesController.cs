using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.Api;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Controllers.Api;

public class FabricTypesController : ApiControllerBase
{
    private readonly ApplicationDbContext _context;

    public FabricTypesController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _context.FabricTypes
            .OrderBy(f => f.Name)
            .Select(f => new { f.Id, f.Name, f.Description })
            .ToListAsync();

        return ApiOk(types);
    }
}
