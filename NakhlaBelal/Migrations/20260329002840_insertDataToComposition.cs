using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class insertDataToComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("insert into Compositions (Name, Description, ImageUrl) values ('Darb', 'Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Proin risus. Praesent lectus.', 'http://dummyimage.com/212x100.png/cc0000/ffffff');\r\ninsert into Compositions (Name, Description, ImageUrl) values ('Laurence', 'Aenean lectus. Pellentesque eget nunc. Donec quis orci eget orci vehicula condimentum.', 'http://dummyimage.com/250x100.png/cc0000/ffffff');\r\ninsert into Compositions (Name, Description, ImageUrl) values ('Earlie', 'Nullam sit amet turpis elementum ligula vehicula consequat. Morbi a ipsum. Integer a nibh.', 'http://dummyimage.com/116x100.png/dddddd/000000');\r\ninsert into Compositions (Name, Description, ImageUrl) values ('Cleon', 'Quisque porta volutpat erat. Quisque erat eros, viverra eget, congue eget, semper rutrum, nulla. Nunc purus.', 'http://dummyimage.com/133x100.png/ff4444/ffffff');\r\ninsert into Compositions (Name, Description, ImageUrl) values ('Sarge', 'In congue. Etiam justo. Etiam pretium iaculis justo.', 'http://dummyimage.com/174x100.png/5fa2dd/ffffff');\r\n");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE Table Compositions");
        }
    }
}
