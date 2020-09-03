using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
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

    }
}
