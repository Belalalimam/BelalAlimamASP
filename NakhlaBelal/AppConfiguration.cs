using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NakhlaBelal.Api.Services;
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
        public static void RegisterConfig(this IServiceCollection services, string connection, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(option =>
            {
                //option.UseSqlServer(builder.Configuration.GetSection("ConnectionStrings")["DefaultConnection"]);
                //option.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]);
                option.UseSqlServer(connection);
            });

            // ========== WhatsApp Business Cloud API ==========
            services.Configure<WhatsAppSettings>(configuration.GetSection("WhatsApp"));
            services.AddHttpClient<IWhatsAppService, WhatsAppService>();
            // ==================================================

            // ========== خدمة إعدادات الموقع ==========
            services.AddSingleton<ISiteSettingsService, SiteSettingsService>();
            // ==========================================

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
            services.AddScoped<IAdminEmailNotifier, AdminEmailNotifier>();


            services.AddScoped<IDBInitializer, DBInitializer>();
        }

        public static void RegisterApiConfig(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

            services.AddAuthentication()
                    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ValidateIssuer = true,
                            ValidIssuer = jwtSection["Issuer"],
                            ValidateAudience = true,
                            ValidAudience = jwtSection["Audience"],
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.Zero
                        };
                    });

            services.AddScoped<IJwtTokenService, JwtTokenService>();

            services.AddCors(options =>
            {
                options.AddPolicy("MobileApp", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Nakhla Fabric Store API",
                    Version = "v1",
                    Description = "REST API for Nakhla mobile app"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {your-token-here}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }
    }
}
