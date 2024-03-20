using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationMessageAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationEmailAttachment_Notification_NotificationId",
                table: "NotificationEmailAttachment");

            migrationBuilder.DropIndex(
                name: "IX_NotificationEmailAttachment_NotificationId",
                table: "NotificationEmailAttachment");

            migrationBuilder.DropColumn(
                name: "NotificationId",
                table: "NotificationEmailAttachment");

            migrationBuilder.AddColumn<string>(
                name: "NotificationMessageId",
                table: "NotificationEmailAttachment",
                type: "varchar(128)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmailAttachment_NotificationMessageId",
                table: "NotificationEmailAttachment",
                column: "NotificationMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationEmailAttachment_NotificationMessage_Notification~",
                table: "NotificationEmailAttachment",
                column: "NotificationMessageId",
                principalTable: "NotificationMessage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationEmailAttachment_NotificationMessage_Notification~",
                table: "NotificationEmailAttachment");

            migrationBuilder.DropIndex(
                name: "IX_NotificationEmailAttachment_NotificationMessageId",
                table: "NotificationEmailAttachment");

            migrationBuilder.DropColumn(
                name: "NotificationMessageId",
                table: "NotificationEmailAttachment");

            migrationBuilder.AddColumn<string>(
                name: "NotificationId",
                table: "NotificationEmailAttachment",
                type: "varchar(128)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmailAttachment_NotificationId",
                table: "NotificationEmailAttachment",
                column: "NotificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationEmailAttachment_Notification_NotificationId",
                table: "NotificationEmailAttachment",
                column: "NotificationId",
                principalTable: "Notification",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
