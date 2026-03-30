using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class EditInProductTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableColors",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailableColors",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
