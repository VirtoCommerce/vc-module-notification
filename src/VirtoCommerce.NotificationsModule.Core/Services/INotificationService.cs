using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Result;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// The service has a logic query/save of notification to Database
    /// </summary>
    public interface INotificationService
    {
        Task<Notification[]> GetByIdsAsync(string[] ids, string responseGroup = null);

        Task SaveChangesAsync(Notification[] notifications);

        /// <summary>
        /// Reset template to default
        /// </summary>
        /// <param name="notificationId">Current notification Id</param>
        /// <param name="templateId">Template Id</param>
        /// <returns>Return true if template reseted; false otherwise</returns>
        Task<OperationResult> ResetTemplate(string notificationId, string templateId);
    }
}
