using System;
using System.Collections.Generic;
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
        public NotificationRepository(NotificationDbContext dbContext)
            : base(dbContext)
        {
        }

        public IQueryable<EmailNotificationEntity> EmailNotifications => DbContext.Set<EmailNotificationEntity>();
        public IQueryable<SmsNotificationEntity> SmsNotifications => DbContext.Set<SmsNotificationEntity>();
        public IQueryable<NotificationEntity> Notifications => DbContext.Set<NotificationEntity>();
        public IQueryable<NotificationMessageEntity> NotificationMessages => DbContext.Set<NotificationMessageEntity>();
        public IQueryable<NotificationLayoutEntity> NotificationLayouts => DbContext.Set<NotificationLayoutEntity>();

        public async Task<IList<NotificationEntity>> GetByIdsAsync(IList<string> ids, string responseGroup)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Array.Empty<NotificationEntity>();
            }

            var result = await Notifications
                .Where(x => ids.Contains(x.Id))
                .OrderBy(x => x.Type)
                .ToListAsync();

            if (result.Any())
            {
                var existingIds = result.Select(x => x.Id).ToArray();
                await LoadNotificationDependenciesAsync(existingIds, responseGroup);
            }

            return result;
        }

        public async Task<IList<NotificationEntity>> GetByNotificationsAsync(IList<Notification> notifications, string responseGroup)
        {
            // TODO: https://docs.microsoft.com/en-us/ef/core/querying/client-eval
            var notificationKeys = notifications
                .Select(x => new { Key1 = x.Type, Key2 = x.TenantIdentity.Id, Key3 = x.TenantIdentity.Type })
                .ToList();

            var result = Notifications
                .AsEnumerable()
                .Where(x => notificationKeys.Contains(new { Key1 = x.Type, Key2 = x.TenantId, Key3 = x.TenantType }))
                .OrderBy(x => x.Type)
                .ToList();

            var existingIds = result.Select(x => x.Id).ToArray();
            await LoadNotificationDependenciesAsync(existingIds, responseGroup);

            return result.ToList();
        }

        public async Task<IList<NotificationMessageEntity>> GetMessagesByIdsAsync(IList<string> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Array.Empty<NotificationMessageEntity>();
            }

            var result = await NotificationMessages
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            if (result.Any())
            {
                var existingIds = result.Select(x => x.NotificationId).ToArray();
                await Notifications.Where(x => existingIds.Contains(x.Id)).OrderBy(x => x.Type).LoadAsync();
                await DbContext.Set<EmailAttachmentEntity>().Where(t => ids.Contains(t.NotificationMessageId)).LoadAsync();
            }

            return result;
        }

        public async Task<IList<NotificationLayoutEntity>> GetNotificationLayoutsByIdsAsync(IList<string> ids)
        {
            return await NotificationLayouts
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        protected virtual async Task LoadNotificationDependenciesAsync(IList<string> ids, string responseGroup)
        {
            var notificationResponseGroup = EnumUtility.SafeParseFlags(responseGroup, NotificationResponseGroup.Full);

            if (ids.Any())
            {
                if (notificationResponseGroup.HasFlag(NotificationResponseGroup.WithTemplates))
                {
                    await DbContext.Set<NotificationTemplateEntity>().Where(t => ids.Contains(t.NotificationId)).LoadAsync();
                }

                if (notificationResponseGroup.HasFlag(NotificationResponseGroup.WithRecipients))
                {
                    await DbContext.Set<NotificationEmailRecipientEntity>().Where(t => ids.Contains(t.NotificationId)).LoadAsync();
                }
            }
        }
    }
}
