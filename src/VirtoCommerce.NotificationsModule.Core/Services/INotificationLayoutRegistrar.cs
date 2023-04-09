using System;
using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    public interface INotificationLayoutRegistrar
    {
        IEnumerable<NotificationLayout> AllRegisteredNotificationsLayout { get; }
        void RegisterNotificationLayoutWithParams(string name, string template);
        void RegisterNotificationLayoutWithTemplateFromPath(string name, string path);
    }
}
