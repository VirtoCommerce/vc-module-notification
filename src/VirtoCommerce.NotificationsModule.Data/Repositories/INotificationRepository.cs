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
        IQueryable<NotificationMessageEntity> NotifcationMessages { get; }
        IQueryable<NotificationLayoutEntity> NotificationLayouts { get; }
        Task<NotificationEntity[]> GetByIdsAsync(string[] ids, string responseGroup);
        Task<NotificationEntity[]> GetByNotificationsAsync(Notification[] notifications, string responseGroup);
        Task<NotificationMessageEntity[]> GetMessagesByIdsAsync(string[] ids);
        Task<IEnumerable<NotificationLayoutEntity>> GetNotificationLayoutsByIdsAsync(IEnumerable<string> ids);
    }
}
