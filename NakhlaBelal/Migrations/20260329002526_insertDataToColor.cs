using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class insertDataToColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("insert into Colors (Name, Description, HexCode, ImageUrl) values ('Red', 'Maecenas tristique, est et tempus semper, est quam pharetra magna, ac consequat metus sapien ut nunc. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Mauris viverra diam vitae quam. Suspendisse potenti.', '#1322ee', 'http://dummyimage.com/150x100.png/ff4444/ffffff');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Pink', 'Maecenas leo odio, condimentum id, luctus nec, molestie sed, justo. Pellentesque viverra pede ac diam. Cras pellentesque volutpat dui.', '#b3971d', 'http://dummyimage.com/122x100.png/5fa2dd/ffffff');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Maroon', 'In congue. Etiam justo. Etiam pretium iaculis justo.', '#ecd029', 'http://dummyimage.com/215x100.png/ff4444/ffffff');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Turquoise', 'Vestibulum ac est lacinia nisi venenatis tristique. Fusce congue, diam id ornare imperdiet, sapien urna pretium nisl, ut volutpat sapien arcu sed augue. Aliquam erat volutpat.', '#47e3a1', 'http://dummyimage.com/122x100.png/dddddd/000000');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Purple', 'Praesent id massa id nisl venenatis lacinia. Aenean sit amet justo. Morbi ut odio.', '#905c76', 'http://dummyimage.com/177x100.png/dddddd/000000');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Purple', 'Morbi porttitor lorem id ligula. Suspendisse ornare consequat lectus. In est risus, auctor sed, tristique in, tempus sit amet, sem.', '#3e8144', 'http://dummyimage.com/102x100.png/ff4444/ffffff');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Crimson', 'Duis aliquam convallis nunc. Proin at turpis a pede posuere nonummy. Integer non velit.', '#e9d2b3', 'http://dummyimage.com/197x100.png/5fa2dd/ffffff');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Maroon', 'Nulla ut erat id mauris vulputate elementum. Nullam varius. Nulla facilisi.', '#4f00f6', 'http://dummyimage.com/127x100.png/cc0000/ffffff');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Maroon', 'Vestibulum ac est lacinia nisi venenatis tristique. Fusce congue, diam id ornare imperdiet, sapien urna pretium nisl, ut volutpat sapien arcu sed augue. Aliquam erat volutpat.', '#8d078b', 'http://dummyimage.com/216x100.png/ff4444/ffffff');\r\ninsert into Colors (Name, Description, HexCode, ImageUrl) values ('Blue', 'Cras mi pede, malesuada in, imperdiet et, commodo vulputate, justo. In blandit ultrices enim. Lorem ipsum dolor sit amet, consectetuer adipiscing elit.', '#186808', 'http://dummyimage.com/249x100.png/dddddd/000000');\r\n");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE Table Colors");
        }
    }
}
