using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUnitAndMinQty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Products', 'MinimumQuantity') IS NULL
                    ALTER TABLE [Products] ADD [MinimumQuantity] int NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Products', 'QuantityStep') IS NULL
                    ALTER TABLE [Products] ADD [QuantityStep] int NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Products', 'Unit') IS NULL
                    ALTER TABLE [Products] ADD [Unit] int NOT NULL DEFAULT 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Products', 'MinimumQuantity') IS NOT NULL
                    ALTER TABLE [Products] DROP COLUMN [MinimumQuantity];
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Products', 'QuantityStep') IS NOT NULL
                    ALTER TABLE [Products] DROP COLUMN [QuantityStep];
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('dbo.Products', 'Unit') IS NOT NULL
                    ALTER TABLE [Products] DROP COLUMN [Unit];
            ");
        }
    }
}
