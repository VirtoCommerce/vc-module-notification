using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.SqlServer.Migrations
{
    public partial class FixSmsNotificationMessageLength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "NotificationTemplate",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1600)",
                oldMaxLength: 1600,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "NotificationMessage",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1600)",
                oldMaxLength: 1600,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "NotificationTemplate",
                type: "nvarchar(1600)",
                maxLength: 1600,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "NotificationMessage",
                type: "nvarchar(1600)",
                maxLength: 1600,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
