using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.NotificationsModule.Data.PostgreSql.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TenantType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Kind = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OuterId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    From = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    To = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLayout",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Template = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLayout", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationEmailAttachment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    MimeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Size = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NotificationId = table.Column<string>(type: "character varying(128)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEmailAttachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationEmailAttachment_Notification_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationEmailRecipient",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RecipientType = table.Column<int>(type: "integer", nullable: false),
                    NotificationId = table.Column<string>(type: "character varying(128)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEmailRecipient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationEmailRecipient_Notification_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMessage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TenantType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NotificationType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SendAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxSendAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastSendError = table.Column<string>(type: "text", nullable: true),
                    LastSendAttemptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SendDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NotificationId = table.Column<string>(type: "character varying(128)", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    From = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    To = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CC = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    BCC = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationMessage_Notification_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplate",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OuterId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    NotificationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    Sample = table.Column<string>(type: "text", nullable: true),
                    NotificationLayoutId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationTemplate_Notification_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationTemplate_NotificationLayout_NotificationLayoutId",
                        column: x => x.NotificationLayoutId,
                        principalTable: "NotificationLayout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_Type_TenantId_TenantType",
                table: "Notification",
                columns: new[] { "Type", "TenantId", "TenantType" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmailAttachment_NotificationId",
                table: "NotificationEmailAttachment",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmailRecipient_NotificationId",
                table: "NotificationEmailRecipient",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMessage_NotificationId",
                table: "NotificationMessage",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplate_NotificationId",
                table: "NotificationTemplate",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationTemplate_NotificationLayoutId",
                table: "NotificationTemplate",
                column: "NotificationLayoutId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationEmailAttachment");

            migrationBuilder.DropTable(
                name: "NotificationEmailRecipient");

            migrationBuilder.DropTable(
                name: "NotificationMessage");

            migrationBuilder.DropTable(
                name: "NotificationTemplate");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "NotificationLayout");
        }
    }
}
