using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutRegistrar : INotificationLayoutRegistrar
    {
        private readonly IServiceProvider _serviceProvider;

        public IEnumerable<NotificationLayout> AllRegisteredNotificationsLayout
        {
            get
            {
                return AbstractTypeFactory<NotificationLayout>.AllTypeInfos.Select(x => AbstractTypeFactory<NotificationLayout>.TryCreateInstance(x.TypeName));
            }
        }

        public NotificationLayoutRegistrar(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public NotificationLayoutBuilder NotificationLayout<TNotificationLayout>() where TNotificationLayout : NotificationLayout
        {
            var typeInfo = AbstractTypeFactory<NotificationLayout>.RegisterType<TNotificationLayout>();
            var builder = new NotificationLayoutBuilder(_serviceProvider, typeof(TNotificationLayout), typeInfo.Factory);
            typeInfo.WithFactory(() => builder.Build());
            return builder;
        }

        public NotificationLayoutBuilder RegisterNotificationLayout<TNotificationLayout>(Func<NotificationLayout> factory = null) where TNotificationLayout : NotificationLayout
        {
            var typeInfo = AbstractTypeFactory<NotificationLayout>.RegisterType<TNotificationLayout>();
            var builder = new NotificationLayoutBuilder(_serviceProvider, typeof(TNotificationLayout), factory);
            typeInfo.WithFactory(() => builder.Build());
            return builder;
        }
    }
}
