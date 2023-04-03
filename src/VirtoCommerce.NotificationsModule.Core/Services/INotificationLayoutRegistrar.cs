using System;
using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    public interface INotificationLayoutRegistrar
    {
        IEnumerable<NotificationLayout> AllRegisteredNotificationsLayout { get; }
        NotificationLayoutBuilder NotificationLayout<TNotificationLayout>() where TNotificationLayout : NotificationLayout;
        NotificationLayoutBuilder RegisterNotificationLayout<TNotificationLayout>(Func<NotificationLayout> factory = null) where TNotificationLayout : NotificationLayout;
    }
}
