using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class insertDataToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("insert into Products (Name, MainImage, ShortDescription, Description, SKU, CostPrice, Price, SpecialPrice, SpecialPriceStart, SpecialPriceEnd, Discount, StockQuantity, LowStockThreshold, ManageStock, IsInStock, BackordersAllowed, MetaTitle, MetaDescription, Weight, Length, Width, Height, Status, IsFeatured, IsNew, IsBestSeller, DisplayOrder, ViewsCount, Traffic, Rate, ReviewCount, AverageRating, CategoryId, BrandId, ColorId, HasVariations, IsDownloadable, IsVirtual, RequiresShipping, Taxable, CreatedAt, UpdatedAt, IsDeleted, DeletedAt, FabricTypeId, Slug, CreatedBy) values ('Electric Egg Cooker', '1.jpg', 'Etiam faucibus cursus urna. Ut tellus.', 'Sed ante. Vivamus tortor. Duis mattis egestas metus.', 'DuisMattisEgestas.xls', 52.43, 977.42, 480.4, '5/10/2027', '7/26/2026', 71.95, 9333, 133, 0, 1, 1, 'Ms', 'Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Proin interdum mauris non ligula pellentesque ultrices.', 9.6, 110.08, 128.37, 72.35, 0, 0, 0, 1, 6919, 1132793, 4186484, 0, 89218, 1.96, 1, 5, 3, 0, 1, 0, 1, 0, '3/15/2027', '12/3/2025', 1, '12/13/2026', 5, 'libero ut', 'ac');\r\n\r\n");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE Table Products");
        }
    }
}
