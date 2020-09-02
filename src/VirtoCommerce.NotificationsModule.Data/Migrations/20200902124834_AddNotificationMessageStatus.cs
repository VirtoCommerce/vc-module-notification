using Microsoft.EntityFrameworkCore.Migrations;

namespace VirtoCommerce.NotificationsModule.Data.Migrations
{
    public partial class AddNotificationMessageStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "NotificationMessage",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(@"UPDATE [NotificationMessage] SET 
                    [NotificationMessage].[Status] = CASE
						WHEN ([SendDate] is not null) THEN 'Sent' 
						WHEN ([SendDate] is null AND [LastSendError] is null) THEN 'Pending'
						WHEN ([SendDate] is null AND [LastSendError] is not null) THEN 'Error' 
						ELSE 'Pending'
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
