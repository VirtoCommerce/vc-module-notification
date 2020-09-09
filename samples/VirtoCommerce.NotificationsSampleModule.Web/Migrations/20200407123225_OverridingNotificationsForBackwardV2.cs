using Microsoft.EntityFrameworkCore.Migrations;

namespace VirtoCommerce.NotificationsSampleModule.Web.Migrations
{
    public partial class OverridingNotificationsForBackwardV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //need to define derived notifications where has own types and convert the types to based type (like as SampleEmailNotification)
            migrationBuilder.Sql(@"
                 DECLARE @base varchar(128) = 'SampleEmailNotification';
                 DECLARE @extend varchar(128) = 'ExtendedSampleEmailNotification';
                 IF (EXISTS (SELECT *
                 FROM INFORMATION_SCHEMA.TABLES
                 WHERE TABLE_NAME = '__MigrationHistory'))
                    BEGIN
                        BEGIN
                            UPDATE [NotificationMessage] SET
                                [NotificationMessage].NotificationId = [Notification].Id,
                                [NotificationMessage].NotificationType = [Notification].Type
                            FROM [Notification]
                            WHERE [NotificationMessage].NotificationType = @extend
                            AND [Notification].Type = @base
                        END
                        BEGIN
                            UPDATE [NotificationTemplate] SET
                                [NotificationTemplate].NotificationId = n.Id
                            FROM [Notification] n
                            WHERE [NotificationTemplate].NotificationId = (SELECT TOP 1 Id FROM [Notification] WHERE Type = @extend)
                            AND n.Type = @base
                        END
                        BEGIN
                            DELETE FROM [Notification] WHERE [Type] = @extend
                        END
                    END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not needed
        }
    }
}
