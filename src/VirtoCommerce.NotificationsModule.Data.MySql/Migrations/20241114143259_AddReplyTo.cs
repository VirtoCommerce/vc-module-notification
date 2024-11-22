using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddReplyTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReplyTo",
                table: "NotificationMessage",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReplyTo",
                table: "Notification",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "NotificationMessage");

            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "Notification");
        }
    }
}
