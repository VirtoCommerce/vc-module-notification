using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsSampleModule.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialTwitterNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Post", table: "Notification", maxLength: 1024, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Post", table: "Notification");
        }
    }
}
