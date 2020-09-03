using System;
using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// The registrar is for registration a type
    /// </summary>
    public interface INotificationRegistrar
    {
        IEnumerable<Notification> AllRegisteredNotifications { get; }
        NotificationBuilder Notification<TNotification>() where TNotification : Notification;
        NotificationBuilder RegisterNotification<TNotification>(Func<Notification> factory = null) where TNotification : Notification;
        NotificationBuilder OverrideNotificationType<TOldNotificationType, TNewNotificationType>(Func<Notification> factory = null) where TOldNotificationType : Notification where TNewNotificationType : Notification;
    }
}
