using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services;

public interface INotificationBrandService
{
    /// <summary>
    /// Returns the email brand of the store: each value set on the store, otherwise the global value, otherwise the default.
    /// Without a store id returns the global brand.
    /// </summary>
    Task<NotificationBrand> GetBrandAsync(string storeId);
}
