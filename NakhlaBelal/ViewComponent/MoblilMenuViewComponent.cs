using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess; // تأكد من المسار الصحيح للـ DbContext

public class MobilMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;

    public MobilMenuViewComponent(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // جلب البيانات من الجداول التي ذكرتها
        var viewModel = new MobilMenuViewModel
        {
            FabricTypes = await _db.FabricTypes.ToListAsync(),
            ProjectCategories = await _db.ProjectCategories.ToListAsync(),
            Colors = await _db.Colors.ToListAsync(),
            Compositions = await _db.Compositions.ToListAsync()
        };

        return View(viewModel);
    }
}

// ViewModel بسيط لنقل البيانات للواجهة
public class MobilMenuViewModel
{
    public List<NakhlaBelal.Models.FabricType> FabricTypes { get; set; }
    public List<NakhlaBelal.Models.ProjectCategory> ProjectCategories { get; set; }
    public List<NakhlaBelal.Models.Color> Colors { get; set; }
    public List<NakhlaBelal.Models.Composition> Compositions { get; set; }
}