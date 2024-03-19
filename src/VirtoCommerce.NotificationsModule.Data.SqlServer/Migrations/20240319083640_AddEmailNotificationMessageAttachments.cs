using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.SqlServer.VirtoCommerce.NotificationsModule.Data.SqlServer
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

            migrationBuilder.RenameColumn(
                name: "NotificationId",
                table: "NotificationEmailAttachment",
                newName: "NotificationMessageId");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationEmailAttachment_NotificationId",
                table: "NotificationEmailAttachment",
                newName: "IX_NotificationEmailAttachment_NotificationMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationEmailAttachment_NotificationMessage_NotificationMessageId",
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
                name: "FK_NotificationEmailAttachment_NotificationMessage_NotificationMessageId",
                table: "NotificationEmailAttachment");

            migrationBuilder.RenameColumn(
                name: "NotificationMessageId",
                table: "NotificationEmailAttachment",
                newName: "NotificationId");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationEmailAttachment_NotificationMessageId",
                table: "NotificationEmailAttachment",
                newName: "IX_NotificationEmailAttachment_NotificationId");

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
