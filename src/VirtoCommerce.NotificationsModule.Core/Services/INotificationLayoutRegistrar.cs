using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    public interface INotificationLayoutRegistrar
    {
        IEnumerable<NotificationLayout> AllRegisteredLayouts { get; }

        NotificationLayout GetByName(string name) =>
            AllRegisteredLayouts.FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));

        void RegisterLayout(string name, string template, bool saveChanges = false);
        void RegisterLayoutWithTemplateFromPath(string name, string path, bool saveChanges = false);

        /// <summary>
        /// No-op. Predefined layouts are kept in memory and no longer persisted to the database.
        /// Kept for backward compatibility.
        /// </summary>
        void SaveChanges() { }
    }
}
