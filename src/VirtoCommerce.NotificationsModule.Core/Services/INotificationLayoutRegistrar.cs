using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    public interface INotificationLayoutRegistrar
    {
        IEnumerable<NotificationLayout> AllRegisteredLayouts { get; }
        void RegisterLayout(string name, string template);
        void RegisterAndSaveLayout(string name, string template);
        void RegisterLayoutWithTemplateFromPath(string name, string path);
        void SaveChanges();
    }
}
