using Microsoft.EntityFrameworkCore.Migrations;
using VirtoCommerce.Platform.Data.PostgreSql.Extensions;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseInsensitiveCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateCaseInsensitiveCollationIfNotExists();

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "NotificationMessage",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "NotificationMessage",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReplyTo",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NotificationType",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastSendError",
                table: "NotificationMessage",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "From",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CC",
                table: "NotificationMessage",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "NotificationMessage",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BCC",
                table: "NotificationMessage",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NotificationLayout",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notification",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Notification",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "NotificationMessage",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "NotificationMessage",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "ReplyTo",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "NotificationType",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "LastSendError",
                table: "NotificationMessage",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "From",
                table: "NotificationMessage",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CC",
                table: "NotificationMessage",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "NotificationMessage",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "BCC",
                table: "NotificationMessage",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NotificationLayout",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notification",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "Notification",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldCollation: "case_insensitive");
        }
    }
}
