using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class insertDataToCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("insert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Pixonyx', 'Aenean sit amet justo. Morbi ut odio. Cras mi pede, malesuada in, imperdiet et, commodo vulputate, justo.', 0, 'Remove imp dev-metat/tar', 'Honorable', 33, '3/29/2027', 'Tuna Fish (canned)', 1);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Oyoba', 'Duis at velit eu est congue elementum. In hac habitasse platea dictumst.', 0, 'Creat esophagastr sphinc', 'Ms', 964, '1/9/2027', 'Classic Leather Wallet', 0);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Skiba', 'Vivamus tortor.', 1, 'Pass musculosk exer NEC', 'Ms', 774, '2/2/2030', 'Handheld Garment Steamer', 1);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Photolist', 'Mauris lacinia sapien quis libero.', 1, 'Total pinealectomy', 'Mr', 795, '3/30/2028', 'Blender Bottle', 0);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Youspan', 'Quisque arcu libero, rutrum ac, lobortis vel, dapibus at, diam. Nam tristique tortor eu pede.', 0, 'Labial frenumectomy', 'Dr', 189, '2/2/2021', 'Car Phone Mount', 1);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Oyondu', 'Aenean sit amet justo. Morbi ut odio.', 0, 'Repair facial weakness', 'Mrs', 249, '6/12/2029', 'Italian Herb Balsamic Marinade', 0);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Meetz', 'Morbi a ipsum. Integer a nibh.', 0, 'Pectus deformity repair', 'Mr', 241, '8/13/2029', 'Bamboo Toothbrush Holder', 0);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Babbleblab', 'Etiam justo. Etiam pretium iaculis justo. In hac habitasse platea dictumst.', 0, 'Labial frenumectomy', 'Dr', 542, '3/9/2027', 'Puffer Winter Coat', 1);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Trupe', 'Quisque erat eros, viverra eget, congue eget, semper rutrum, nulla. Nunc purus. Phasellus in felis.', 1, 'Remov vaginal diaphragm', 'Ms', 831, '4/4/2020', 'Electric Griddle with Lid', 0);\r\ninsert into Categorise (Name, Description, Status, Slug, MetaTitle, DisplayOrder, CreatedAt, CreatedBy, IsDeleted) values ('Twiyo', 'Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Donec pharetra, magna vestibulum aliquet ultrices, erat tortor sollicitudin mi, sit amet lobortis sapien sapien non mi.', 0, 'Ins part disc pros lumb', 'Mrs', 414, '2/7/2022', 'Window Bird Feeder', 1);\r\n");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("TRUNCATE Table Categorise");
        }
    }
}
