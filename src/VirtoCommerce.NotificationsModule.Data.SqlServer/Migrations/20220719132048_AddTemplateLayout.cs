using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.SqlServer.Migrations
{
    public partial class AddTemplateLayout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationLayoutId",
                table: "NotificationTemplate",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationLayout",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Template = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLayout", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplate_NotificationLayoutId",
                table: "NotificationTemplate",
                column: "NotificationLayoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationTemplate_NotificationLayout_NotificationLayoutId",
                table: "NotificationTemplate",
                column: "NotificationLayoutId",
                principalTable: "NotificationLayout",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationTemplate_NotificationLayout_NotificationLayoutId",
                table: "NotificationTemplate");

            migrationBuilder.DropTable(
                name: "NotificationLayout");

            migrationBuilder.DropIndex(
                name: "IX_NotificationTemplate_NotificationLayoutId",
                table: "NotificationTemplate");

            migrationBuilder.DropColumn(
                name: "NotificationLayoutId",
                table: "NotificationTemplate");
        }
    }
}
