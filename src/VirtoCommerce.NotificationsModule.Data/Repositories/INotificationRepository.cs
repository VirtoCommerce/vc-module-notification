using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Repositories
{
    public interface INotificationRepository : IRepository
    {
        IQueryable<NotificationEntity> Notifications { get; }
        IQueryable<NotificationMessageEntity> NotificationMessages { get; }
        IQueryable<NotificationLayoutEntity> NotificationLayouts { get; }

        Task<IList<NotificationEntity>> GetByIdsAsync(IList<string> ids, string responseGroup);
        Task<IList<NotificationEntity>> GetByNotificationsAsync(IList<Notification> notifications, string responseGroup);
        Task<IList<NotificationMessageEntity>> GetMessagesByIdsAsync(IList<string> ids);
        Task<IList<NotificationLayoutEntity>> GetNotificationLayoutsByIdsAsync(IList<string> ids);
    }
}
