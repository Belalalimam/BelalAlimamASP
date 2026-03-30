using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using NakhlaBelal.DataAccess;
using NakhlaBelal.Models;
using NakhlaBelal.Repositories;
using NakhlaBelal.Repositories.IRepositories;
using NakhlaBelal.Utitlies;
using NakhlaBelal.Utitlies.DBInitilizer;

namespace NakhlaBelal
{
    public static class AppConfiguration
    {
        public static void RegisterConfig(this IServiceCollection services, string connection)
        {
            services.AddDbContext<ApplicationDbContext>(option =>
            {
                //option.UseSqlServer(builder.Configuration.GetSection("ConnectionStrings")["DefaultConnection"]);
                //option.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]);
                option.UseSqlServer(connection);
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(option =>
            {
                option.User.RequireUniqueEmail = true;
                option.Password.RequiredLength = 8;
                option.Password.RequireNonAlphanumeric = false;
                option.SignIn.RequireConfirmedEmail = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login"; // Default login path
                options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Default access denied path
            });

            services.AddTransient<IEmailSender, EmailSender>();

            services.AddScoped<IRepository<Category>, Repository<Category>>();
            services.AddScoped<IRepository<Brand>, Repository<Brand>>();
            services.AddScoped<IRepository<Product>, Repository<Product>>();
            services.AddScoped<IRepository<ProductImage>, Repository<ProductImage>>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IRepository<UserOTP>, Repository<UserOTP>>();
            services.AddScoped<IRepository<Cart>, Repository<Cart>>();
            services.AddScoped<IRepository<Promotion>, Repository<Promotion>>();
            services.AddScoped<IRepository<Order>, Repository<Order>>();
            services.AddScoped<IRepository<OrderItem>, Repository<OrderItem>>();
            services.AddScoped<IRepository<Color>, Repository<Color>>();
            services.AddScoped<IRepository<FabricType>, Repository<FabricType>>();
            services.AddScoped<IRepository<ProjectCategory>, Repository<ProjectCategory>>();


            services.AddScoped<IDBInitializer, DBInitializer>();
        }
    }
}
