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
                await LoadNotificationDependenciesAsync(ids, responseGroup);
            }
            return result;
        }

        public async Task<NotificationEntity[]> GetByNotificationsAsync(Notification[] notifications, string responseGroup)
        {
            //TODO: https://docs.microsoft.com/en-us/ef/core/querying/client-eval
            var notificationKeys = notifications.Select(x => new { Key1 = x.Type, Key2 = x.TenantIdentity.Id, Key3 = x.TenantIdentity.Type }).ToList();
            var result = Notifications.AsEnumerable().Where(x => notificationKeys.Contains(new { Key1 = x.Type, Key2 = x.TenantId, Key3 = x.TenantType })).OrderBy(x => x.Type).ToList();
            var ids = result.Select(x => x.Id).ToArray();
            await LoadNotificationDependenciesAsync(ids, responseGroup);

            return result.ToArray();
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


        protected virtual async Task LoadNotificationDependenciesAsync(string[] ids, string responseGroup)
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
