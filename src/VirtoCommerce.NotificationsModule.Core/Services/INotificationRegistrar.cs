using System;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// The registrar is for registration a type
    /// </summary>
    public interface INotificationRegistrar
    {
        NotificationBuilder RegisterNotification<T>(Func<Notification> factory = null) where T : Notification;
    }
}
