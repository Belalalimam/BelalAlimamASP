using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NakhlaBelal.DataAccess;

namespace NakhlaBelal.Utitlies.DBInitilizer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DBInitializer> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public DBInitializer(ApplicationDbContext context, ILogger<DBInitializer> logger, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public void Initialize()
        {
            try
            {
                if (_context.Database.GetPendingMigrations().Any())
                    _context.Database.Migrate();

                if (_roleManager.Roles.IsNullOrEmpty())
                {
                    _roleManager.CreateAsync(new(SD.SUPER_ADMIN_ROLE)).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new(SD.ADMIN_ROLE)).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new(SD.EMPLOYEE_ROLE)).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new(SD.CUSTOMER_ROLE)).GetAwaiter().GetResult();

                    _userManager.CreateAsync(new()
                    {
                        Email = "superadmin@eraasoft.com",
                        UserName = "SuperAdmin",
                        EmailConfirmed = true,
                        FirstName = "Super",
                        LastName = "Admin",
                    }, "Admin123$").GetAwaiter().GetResult();

                    var user = _userManager.FindByNameAsync("SuperAdmin").GetAwaiter().GetResult();
                    _userManager.AddToRoleAsync(user!, SD.SUPER_ADMIN_ROLE).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
            }
        }
    }
}
