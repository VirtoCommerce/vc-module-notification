using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    public interface INotificationLayoutRegistrar
    {
        IEnumerable<NotificationLayout> AllRegisteredLayouts { get; }
        void RegisterLayout(string name, string template, bool saveChanges = false);
        void RegisterLayoutWithTemplateFromPath(string name, string path, bool saveChanges = false);
        void SaveChanges();
    }
}
