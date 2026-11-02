using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services;

public interface INotificationLayoutUsageService
{
    /// <summary>
    /// Returns the notification types whose email templates use the layout, or null if the layout does not exist.
    /// Counts DB templates, predefined templates not overridden by a DB template in the same language,
    /// and, for the default layout, templates without a layout.
    /// </summary>
    Task<NotificationLayoutUsage> GetUsageAsync(string layoutId);
}
