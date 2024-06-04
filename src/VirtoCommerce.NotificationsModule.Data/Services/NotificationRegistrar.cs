using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationRegistrar : INotificationRegistrar
    {
        private readonly IServiceProvider _serviceProvider;

        public IEnumerable<Notification> AllRegisteredNotifications
        {
            get
            {
                return AbstractTypeFactory<Notification>.AllTypeInfos.Select(x => AbstractTypeFactory<Notification>.TryCreateInstance(x.TypeName));
            }
        }

        public NotificationRegistrar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public NotificationBuilder Notification<TNotification>() where TNotification : Notification
        {
            var typeInfo = AbstractTypeFactory<Notification>.RegisterType<TNotification>();
            var builder = new NotificationBuilder(_serviceProvider, typeof(TNotification), typeInfo.Factory);
            typeInfo.WithFactory(() => builder.Build());
            return builder;
        }

        public NotificationBuilder RegisterNotification<TNotification>(Func<Notification> factory = null) where TNotification : Notification
        {
            var typeInfo = AbstractTypeFactory<Notification>.RegisterType<TNotification>();
            var builder = new NotificationBuilder(_serviceProvider, typeof(TNotification), factory);
            typeInfo.WithFactory(() => builder.Build());
            return builder;
        }
        public NotificationBuilder OverrideNotificationType<TOldNotificationType, TNewNotificationType>(Func<Notification> factory = null) where TOldNotificationType : Notification where TNewNotificationType : Notification
        {
            var typeInfo = AbstractTypeFactory<Notification>.OverrideType<TOldNotificationType, TNewNotificationType>();
            var builder = new NotificationBuilder(_serviceProvider, typeof(TNewNotificationType), factory);
            typeInfo.WithFactory(() => builder.Build());

            return builder;
        }

        public void UnregisterNotification<TNotification>() where TNotification : Notification
        {
            var repositoryFactory = _serviceProvider.GetService<Func<INotificationRepository>>();
            using var repository = repositoryFactory();
            var notificationType = nameof(TNotification);
            foreach (var item in repository.Notifications.Where(n => n.Type == notificationType))
            {
                repository.Remove(item);
            }
            repository.UnitOfWork.Commit();
        }
    }
}
