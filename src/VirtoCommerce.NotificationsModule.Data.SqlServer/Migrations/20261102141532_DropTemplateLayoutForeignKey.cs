using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class DropTemplateLayoutForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationTemplate_NotificationLayout_NotificationLayoutId",
                table: "NotificationTemplate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_NotificationTemplate_NotificationLayout_NotificationLayoutId",
                table: "NotificationTemplate",
                column: "NotificationLayoutId",
                principalTable: "NotificationLayout",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
