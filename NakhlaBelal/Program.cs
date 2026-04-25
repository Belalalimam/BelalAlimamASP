using System.Globalization;
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
builder.Services.RegisterApiConfig(builder.Configuration);



//اكتر من لغة 
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// تحديد اللغات المدعومة — نستعمل النسخة الإنجليزية من تنسيق الأرقام في كل الثقافات
// علشان الـ HTML5 number inputs دايماً تبعت "50.00" بنقطة، والـ model binding يقراها صح
CultureInfo BuildCulture(string name)
{
    var culture = new CultureInfo(name);
    culture.NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
    return culture;
}

var supportedCultures = new[] { BuildCulture("en"), BuildCulture("ar") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(supportedCultures[0]),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

var app = builder.Build();


app.UseRequestLocalization(localizationOptions);


// Initialize the database
using var scope = app.Services.CreateScope();
var service = scope.ServiceProvider.GetService<IDBInitializer>();
service!.Initialize();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nakhla API v1"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Move this here to serve static files

app.UseRouting();

app.UseCors("MobileApp");
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
