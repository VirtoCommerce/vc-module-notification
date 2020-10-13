using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace VirtoCommerce.NotificationsModule.Data.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 128, nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(maxLength: 128, nullable: true),
                    TenantType = table.Column<string>(maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    Type = table.Column<string>(maxLength: 128, nullable: true),
                    Kind = table.Column<string>(maxLength: 128, nullable: true),
                    Discriminator = table.Column<string>(maxLength: 128, nullable: false),
                    From = table.Column<string>(maxLength: 128, nullable: true),
                    To = table.Column<string>(maxLength: 128, nullable: true),
                    Number = table.Column<string>(maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationEmailAttachment",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 128, nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(maxLength: 64, nullable: true),
                    FileName = table.Column<string>(maxLength: 512, nullable: true),
                    Url = table.Column<string>(maxLength: 2048, nullable: true),
                    MimeType = table.Column<string>(maxLength: 50, nullable: true),
                    Size = table.Column<string>(maxLength: 128, nullable: true),
                    LanguageCode = table.Column<string>(maxLength: 10, nullable: true),
                    NotificationId = table.Column<string>(nullable: true)
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
                    Id = table.Column<string>(maxLength: 128, nullable: false),
                    EmailAddress = table.Column<string>(maxLength: 128, nullable: true),
                    RecipientType = table.Column<int>(nullable: false),
                    NotificationId = table.Column<string>(nullable: true)
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
                    Id = table.Column<string>(maxLength: 128, nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(maxLength: 128, nullable: true),
                    TenantType = table.Column<string>(maxLength: 128, nullable: true),
                    NotificationType = table.Column<string>(maxLength: 128, nullable: true),
                    SendAttemptCount = table.Column<int>(nullable: false),
                    MaxSendAttemptCount = table.Column<int>(nullable: false),
                    LastSendError = table.Column<string>(nullable: true),
                    LastSendAttemptDate = table.Column<DateTime>(nullable: true),
                    SendDate = table.Column<DateTime>(nullable: true),
                    LanguageCode = table.Column<string>(maxLength: 10, nullable: true),
                    NotificationId = table.Column<string>(nullable: true),
                    Discriminator = table.Column<string>(maxLength: 128, nullable: false),
                    Subject = table.Column<string>(maxLength: 512, nullable: true),
                    Body = table.Column<string>(nullable: true),
                    Message = table.Column<string>(maxLength: 1600, nullable: true),
                    Number = table.Column<string>(maxLength: 128, nullable: true)
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
                    Id = table.Column<string>(maxLength: 128, nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(maxLength: 64, nullable: true),
                    LanguageCode = table.Column<string>(maxLength: 10, nullable: true),
                    NotificationId = table.Column<string>(maxLength: 128, nullable: true),
                    Discriminator = table.Column<string>(maxLength: 128, nullable: false),
                    Subject = table.Column<string>(maxLength: 512, nullable: true),
                    Body = table.Column<string>(nullable: true),
                    Message = table.Column<string>(maxLength: 1600, nullable: true)
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
                });

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


            migrationBuilder.Sql(@"IF (EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_NAME = '__MigrationHistory'))
                    BEGIN
                        BEGIN
	                        INSERT INTO [Notification]
                                       ([Id]
                                       ,[CreatedDate]
                                       ,[ModifiedDate]
                                       ,[CreatedBy]
                                       ,[ModifiedBy]
                                       ,[TenantId]
                                       ,[TenantType]
                                       ,[IsActive]
                                       ,[Type]
                                       ,[Kind]
                                       ,[Discriminator]
                                       ,[From]
                                       ,[To])
                            SELECT pnt.Id, 
	                            pnt.[CreatedDate], 
	                            pnt.[ModifiedDate], 
	                            pnt.[CreatedBy], 
	                            pnt.[ModifiedBy], 
	                            pnt.[ObjectId], 
	                            pnt.[ObjectTypeId], 
	                            1, 
	                            CASE
		                            WHEN (pnt.[NotificationTypeId] = 'EmailConfirmationNotification') THEN 'ConfirmationEmailNotification' 
		                            WHEN (pnt.[NotificationTypeId] = 'RemindUserNameNotification') THEN 'RemindUserNameEmailNotification' 
		                            WHEN (pnt.[NotificationTypeId] = 'RegistrationInvitationNotification') THEN 'RegistrationInvitationEmailNotification' 
		                            ELSE pnt.[NotificationTypeId]         
	                            END,
	                            CASE 
		                            WHEN (pnt.[NotificationTypeId] LIKE '%EmailNotification%' 
			                            OR pnt.[NotificationTypeId] = 'RemindUserNameNotification'
			                            OR pnt.[NotificationTypeId] = 'RegistrationInvitationNotification'
			                            OR pnt.[NotificationTypeId] = 'EmailConfirmationNotification') 
		                            THEN 'EmailNotification' 
		                            ELSE 
			                            CASE 
				                            WHEN pnt.[NotificationTypeId] LIKE '%SmsNotification%'
				                            THEN 'SmsNotification'
				                            ELSE 'EmailNotification'
			                            END
	                            END,
	                            CASE 
		                            WHEN (pnt.[NotificationTypeId] LIKE '%EmailNotification%' 
			                            OR pnt.[NotificationTypeId] = 'RemindUserNameNotification'
			                            OR pnt.[NotificationTypeId] = 'RegistrationInvitationNotification'
			                            OR pnt.[NotificationTypeId] = 'EmailConfirmationNotification')
		                            THEN 'EmailNotificationEntity' 
		                            ELSE 
			                            CASE 
				                            WHEN pnt.[NotificationTypeId] LIKE '%SmsNotification%'
				                            THEN 'SmsNotificationEntity'
				                            ELSE 'EmailNotificationEntity'
			                            END
	                            END,
	                            null, null
                            FROM 
                            [PlatformNotificationTemplate] pnt
                            INNER JOIN 
                            (
	                            SELECT [NotificationTypeId], MAX([Id]) [Id] FROM [PlatformNotificationTemplate]
	                            GROUP BY [NotificationTypeId]
                            ) pnt2
                            ON pnt.[Id] = pnt2.[Id]
                        END

                        BEGIN
                            INSERT INTO [NotificationTemplate]
                                       ([Id]
                                       ,[CreatedDate]
                                       ,[ModifiedDate]
                                       ,[CreatedBy]
                                       ,[ModifiedBy]
                                       ,[LanguageCode]
                                       ,[Subject]
                                       ,[Body]
                                       ,[Message]
                                       ,[NotificationId]
                                       ,[Discriminator])
                            SELECT pnt.[Id],
                                pnt.[CreatedDate],
                                pnt.[ModifiedDate],
                                pnt.[CreatedBy],
                                pnt.[ModifiedBy],
	                            pnt.[Language],
	                            pnt.[Subject],
                                CASE WHEN pnt.[NotificationTypeId] LIKE '%EmailNotification%' THEN [Body] ELSE '' END,
	                            CASE WHEN pnt.[NotificationTypeId] LIKE '%SmsNotification%' THEN [Body] ELSE '' END,
                                pnt2.Id [NotificationId],
                                CASE 
		                            WHEN pnt.[NotificationTypeId] LIKE '%EmailNotification%' 
		                            THEN 'EmailNotificationTemplateEntity' 
		                            ELSE 
			                            CASE 
				                            WHEN pnt.[NotificationTypeId] LIKE '%SmsNotification%'
				                            THEN 'SmsNotificationTemplateEntity'
				                            ELSE 'EmailNotificationTemplateEntity'
			                            END
	                            END
                            FROM [PlatformNotificationTemplate] pnt
                            INNER JOIN 
                            (
	                            SELECT [NotificationTypeId], MAX([Id]) [Id] FROM [PlatformNotificationTemplate]
	                            GROUP BY [NotificationTypeId]
                            ) pnt2
                            ON pnt.[NotificationTypeId] = pnt2.[NotificationTypeId]
                        END

                        BEGIN
                            INSERT INTO [NotificationMessage]
                                       ([Id]
                                       ,[CreatedDate]
                                       ,[ModifiedDate]
                                       ,[CreatedBy]
                                       ,[ModifiedBy]
                                       ,[TenantId]
                                       ,[TenantType]
                                       ,[NotificationId]
                                       ,[NotificationType]
                                       ,[SendAttemptCount]
                                       ,[MaxSendAttemptCount]
                                       ,[LastSendError]
                                       ,[LastSendAttemptDate]
                                       ,[SendDate]
                                       ,[LanguageCode]
                                       ,[Subject]
                                       ,[Body]
                                       ,[Message],
                                        [Discriminator])
                            SELECT pn.[Id],
	                            pn.[CreatedDate],
	                            pn.[ModifiedDate],
	                            pn.[CreatedBy],
	                            pn.[ModifiedBy],
	                            pn.[ObjectId],
	                            pn.[ObjectTypeId],
	                            pnt2.Id [NotificationId],
	                            pn.[Type],
	                            pn.[AttemptCount],
	                            pn.[MaxAttemptCount],
	                            pn.[LastFailAttemptMessage],
	                            pn.[LastFailAttemptDate],
	                            pn.[SentDate],
	                            pn.[Language],
	                            pn.[Subject],
	                            CASE WHEN pn.[Type] LIKE '%EmailNotification%' THEN [Body] ELSE '' END,
	                            CASE WHEN pn.[Type] LIKE '%SmsNotification%' THEN [Body] ELSE '' END,
	                            CASE 
		                            WHEN pn.[Type] LIKE '%EmailNotification%' 
		                            THEN 'EmailNotificationMessageEntity' 
		                            ELSE 
    		                            CASE 
    			                            WHEN pn.[Type] LIKE '%SmsNotification%'
    			                            THEN 'SmsNotificationMessageEntity'
    			                            ELSE 'EmailNotificationMessageEntity'
    		                            END
	                            END
	                            FROM [PlatformNotification] pn
	                            INNER JOIN 
	                            (
		                            SELECT [NotificationTypeId], MAX([Id]) [Id] FROM [PlatformNotificationTemplate]
		                            GROUP BY [NotificationTypeId]
	                            ) pnt2
	                            on pn.[Type] = pnt2.[NotificationTypeId]
                        END
                        
                    END");
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
        }
    }
}
