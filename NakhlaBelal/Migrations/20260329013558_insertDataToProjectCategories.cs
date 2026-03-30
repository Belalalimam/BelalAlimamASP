using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class insertDataToProjectCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("insert into ProjectCategories (Name, Icon, Description, Slug, ImageUrl) values ('Petr', 'ri-shirt-line', 'Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Vivamus vestibulum sagittis sapien. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.', 'Heinrick', 'http://dummyimage.com/230x100.png/dddddd/000000');\r\ninsert into ProjectCategories (Name, Icon, Description, Slug, ImageUrl) values ('Maynard', 'ri-shirt-line', 'Maecenas leo odio, condimentum id, luctus nec, molestie sed, justo. Pellentesque viverra pede ac diam. Cras pellentesque volutpat dui.', 'Hilliard', 'http://dummyimage.com/204x100.png/ff4444/ffffff');\r\ninsert into ProjectCategories (Name, Icon, Description, Slug, ImageUrl) values ('Zackariah', 'ri-shirt-line', 'Proin leo odio, porttitor id, consequat in, consequat ut, nulla. Sed accumsan felis. Ut at dolor quis odio consequat varius.', 'Delaney', 'http://dummyimage.com/232x100.png/5fa2dd/ffffff');\r\ninsert into ProjectCategories (Name, Icon, Description, Slug, ImageUrl) values ('Kelsey', 'ri-shirt-line', 'Curabitur at ipsum ac tellus semper interdum. Mauris ullamcorper purus sit amet nulla. Quisque arcu libero, rutrum ac, lobortis vel, dapibus at, diam.', 'Franciskus', 'http://dummyimage.com/133x100.png/ff4444/ffffff');\r\ninsert into ProjectCategories (Name, Icon, Description, Slug, ImageUrl) values ('Clement', 'ri-shirt-line', 'Proin eu mi. Nulla ac enim. In tempor, turpis nec euismod scelerisque, quam turpis adipiscing lorem, vitae mattis nibh ligula nec sem.', 'Niko', 'http://dummyimage.com/232x100.png/ff4444/ffffff');\r\n");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE Table ProjectCategories");
        }
    }
}
