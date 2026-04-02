using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace NakhlaBelal.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> productImages { get; set; }
        public DbSet<Category> Categorise { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<UserOTP> UserOTPs { get; set; }
        public DbSet<Cart> Carts { get; set; } = default!;
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductTag> ProductTags { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<FabricType> FabricTypes { get; set; }
        public DbSet<ProjectCategory> ProjectCategories { get; set; }
        public DbSet<Composition> Compositions { get; set; }
        public DbSet<ProductComposition> ProductCompositions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<ProductColor>()
            //    .HasKey(e => new { e.ProductId, e.Color });

            //modelBuilder.Entity<ProductSubImage>()
            //    .HasKey(e => new { e.ProductId, e.Img });

            //new ProductColorEntityTypeConfiguration().Configure(modelBuilder.Entity<ProductColor>());
            //new ProductImgEntityTypeConfiguration().Configure(modelBuilder.Entity<ProductSubImage>());

            //modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductColorEntityTypeConfiguration).Assembly);
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            base.OnModelCreating(modelBuilder);

            // هذا الكود سيقوم بتعيين الدقة لكل الحقول العشرية في المشروع بضربة واحدة
            var decimalProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

            foreach (var property in decimalProperties)
            {
                property.SetColumnType("decimal(18,2)");
            }
        }



    }
}
