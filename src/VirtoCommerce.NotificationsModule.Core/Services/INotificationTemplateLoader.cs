using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// Loads templates from different source than db
    /// </summary>
    public interface INotificationTemplateLoader
    {
        IEnumerable<NotificationTemplate> LoadTemplates(Notification notification);
    }
}
