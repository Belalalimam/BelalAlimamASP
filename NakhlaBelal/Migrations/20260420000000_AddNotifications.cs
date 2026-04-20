using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NakhlaBelal.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Notifications] (
                        [Id]                INT IDENTITY(1,1) NOT NULL,
                        [RecipientUserId]   NVARCHAR(450)   NOT NULL,
                        [Title]             NVARCHAR(200)   NOT NULL,
                        [Message]           NVARCHAR(1000)  NOT NULL,
                        [Type]              NVARCHAR(50)    NOT NULL DEFAULT(''),
                        [Url]               NVARCHAR(500)   NULL,
                        [OrderId]           INT             NULL,
                        [IsRead]            BIT             NOT NULL DEFAULT(0),
                        [ReadAt]            DATETIME2       NULL,
                        [CreatedAt]         DATETIME2       NOT NULL DEFAULT(SYSDATETIME()),
                        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC),
                        CONSTRAINT [FK_Notifications_AspNetUsers_RecipientUserId]
                            FOREIGN KEY ([RecipientUserId]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_Notifications_Orders_OrderId]
                            FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id]) ON DELETE NO ACTION
                    );

                    CREATE INDEX [IX_Notifications_RecipientUserId] ON [dbo].[Notifications]([RecipientUserId]);
                    CREATE INDEX [IX_Notifications_OrderId] ON [dbo].[Notifications]([OrderId]);
                    CREATE INDEX [IX_Notifications_IsRead] ON [dbo].[Notifications]([IsRead]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL
                    DROP TABLE [dbo].[Notifications];
            ");
        }
    }
}
