using Microsoft.EntityFrameworkCore.Migrations;

namespace VirtoCommerce.NotificationsModule.Data.Migrations
{
    public partial class AddNotificationMessageMembers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BCC",
                table: "NotificationMessage",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CC",
                table: "NotificationMessage",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "From",
                table: "NotificationMessage",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "To",
                table: "NotificationMessage",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(@"IF (EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_NAME = '__MigrationHistory'))
                    BEGIN
                        UPDATE [NotificationMessage] SET 
                             [NotificationMessage].[From] = pn.[Sender],
                             [NotificationMessage].[To] = CASE WHEN pn.[Type] LIKE '%EmailNotification%' THEN pn.[Recipient] ELSE
                                                            CASE WHEN pn.[Type] LIKE '%SmsNotification%' THEN NULL ELSE pn.[Recipient] END
                                                          END,
							 [NotificationMessage].[Number] = CASE WHEN pn.[Type] LIKE '%SmsNotification%' THEN pn.[Recipient] ELSE NULL END,
                             [NotificationMessage].[CC] = CASE WHEN pn.[Type] LIKE '%EmailNotification%' THEN pn.[Сс] ELSE NULL END,
                             [NotificationMessage].[BCC] = CASE WHEN pn.[Type] LIKE '%EmailNotification%' THEN pn.[Bcс] ELSE NULL END
                        FROM [NotificationMessage] nm
                        INNER JOIN [PlatformNotification] pn ON pn.Id = nm.Id
                    END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BCC",
                table: "NotificationMessage");

            migrationBuilder.DropColumn(
                name: "CC",
                table: "NotificationMessage");

            migrationBuilder.DropColumn(
                name: "From",
                table: "NotificationMessage");

            migrationBuilder.DropColumn(
                name: "To",
                table: "NotificationMessage");
        }
    }
}
