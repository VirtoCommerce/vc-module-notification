using Microsoft.EntityFrameworkCore.Migrations;

namespace VirtoCommerce.NotificationsModule.Data.Migrations
{
    public partial class AddNotificationMessageStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "NotificationMessage",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"UPDATE [NotificationMessage] SET 
                    [NotificationMessage].[Status] = CASE
						WHEN ([SendDate] is not null) THEN 1 
						WHEN ([SendDate] is null AND [LastSendError] is null) THEN 0
						WHEN ([SendDate] is null AND [LastSendError] is not null) THEN 2 
						ELSE 0
					END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "NotificationMessage");
        }
    }
}
