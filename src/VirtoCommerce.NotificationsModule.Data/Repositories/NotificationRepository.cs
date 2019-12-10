using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.NotificationsModule.Data.Repositories
{
    public class NotificationRepository : DbContextRepositoryBase<NotificationDbContext>, INotificationRepository
    {
        public NotificationRepository(NotificationDbContext dbContext) : base(dbContext)
        {
        }

        public IQueryable<EmailNotificationEntity> EmailNotifications => DbContext.Set<EmailNotificationEntity>();
        public IQueryable<SmsNotificationEntity> SmsNotifications => DbContext.Set<SmsNotificationEntity>();
        public IQueryable<NotificationEntity> Notifications => DbContext.Set<NotificationEntity>();
        public IQueryable<NotificationMessageEntity> NotifcationMessages => DbContext.Set<NotificationMessageEntity>();

        public async Task<NotificationEntity[]> GetByIdsAsync(string[] ids, string responseGroup)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var result = Array.Empty<NotificationEntity>();
            if (ids.Any())
            {
                result = await Notifications.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Type).ToArrayAsync();
                ids = result.Select(x => x.Id).ToArray();
                await GetNestedEntitiesAsync(ids, responseGroup);
            }
            return result;
        }

        public async Task<NotificationEntity[]> GetByNotificationsAsync(Notification[] notifications, string responseGroup)
        {
            //https://github.com/aspnet/EntityFrameworkCore/blob/release/2.2/src/EFCore.Specification.Tests/Query/SimpleQueryTestBase.ResultOperators.cs#L1333
            //TODO: check on ef core 3.1
            var notificationKeys = notifications.Select(x => new { Key1 = x.Type, Key2 = x.TenantIdentity.Id, Key3 = x.TenantIdentity.Type }).ToArray();
            var result = await Notifications.Where(x => notificationKeys.Contains(new { Key1 = x.Type, Key2 = x.TenantId, Key3 = x.TenantType })).OrderBy(x => x.Type).ToArrayAsync();
            var ids = result.Select(x => x.Id).ToArray();
            await GetNestedEntitiesAsync(ids, responseGroup);

            return result;
        }

        public async Task<NotificationMessageEntity[]> GetMessagesByIdsAsync(string[] ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }
            var result = Array.Empty<NotificationMessageEntity>();
            if (ids.Any())
            {
                result = await NotifcationMessages.Where(x => ids.Contains(x.Id)).ToArrayAsync();
                var notificationIds = result.Select(m => m.NotificationId).ToArray();
                if (notificationIds.Any())
                {
                    await Notifications.Where(x => notificationIds.Contains(x.Id)).OrderBy(x => x.Type).LoadAsync();
                }
            }
            return result;
        }


        private async Task GetNestedEntitiesAsync(string[] ids, string responseGroup)
        {
            var notificaionResponseGroup = EnumUtility.SafeParseFlags(responseGroup, NotificationResponseGroup.Full);

            if (ids.Any())
            {
                if (notificaionResponseGroup.HasFlag(NotificationResponseGroup.WithTemplates))
                {
                    await DbContext.Set<NotificationTemplateEntity>().Where(t => ids.Contains(t.NotificationId)).LoadAsync();
                }
                if (notificaionResponseGroup.HasFlag(NotificationResponseGroup.WithAttachments))
                {
                    await DbContext.Set<EmailAttachmentEntity>().Where(t => ids.Contains(t.NotificationId)).LoadAsync();
                }
                if (notificaionResponseGroup.HasFlag(NotificationResponseGroup.WithRecipients))
                {
                    await DbContext.Set<NotificationEmailRecipientEntity>().Where(t => ids.Contains(t.NotificationId)).LoadAsync();
                }
            }
        }
    }
}
