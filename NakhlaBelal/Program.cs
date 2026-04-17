using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Configurations;
using NakhlaBelal.Utitlies.DBInitilizer;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add services to the container.
builder.Services.AddControllersWithViews();

// ================== Session Configuration ==================
builder.Services.AddDistributedMemoryCache(); // Required for Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// ==========================================================

// Register your custom configurations
builder.Services.RegisterConfig(connectionString, builder.Configuration);
builder.Services.RegisterMapsterConfig();



//اكتر من لغة 
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// تحديد اللغات المدعومة
var supportedCultures = new[] { "en", "ar" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en") // اللغة الافتراضية
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

var app = builder.Build();


app.UseRequestLocalization(localizationOptions);


// Initialize the database
using var scope = app.Services.CreateScope();
var service = scope.ServiceProvider.GetService<IDBInitializer>();
service!.Initialize();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Move this here to serve static files

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ===== Add Session Middleware =====
app.UseSession();
// ================================

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=index}/{id?}"
);

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { area = "Customer", controller = "Home", action = "Index" }
);

app.Run();
