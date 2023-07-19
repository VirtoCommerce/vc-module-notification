using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Events;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutService : CrudService<NotificationLayout, NotificationLayoutEntity, NotificationLayoutChangingEvent, NotificationLayoutChangedEvent>, INotificationLayoutService
    {
        public NotificationLayoutService(Func<INotificationRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IEventPublisher eventPublisher)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
        }

        protected override Task<IList<NotificationLayoutEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((INotificationRepository)repository).GetNotificationLayoutsByIdsAsync(ids);
        }
    }
}
