using Microsoft.EntityFrameworkCore.Migrations;

namespace VirtoCommerce.NotificationsModule.Data.SqlServer.Migrations
{
    public partial class AddNotificationOuterId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OuterId",
                table: "NotificationTemplate",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                 name: "OuterId",
                 table: "Notification",
                 maxLength: 128,
                 nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OuterId",
                table: "NotificationTemplate");

            migrationBuilder.DropColumn(
                name: "OuterId",
                table: "Notification");
        }
    }
}
