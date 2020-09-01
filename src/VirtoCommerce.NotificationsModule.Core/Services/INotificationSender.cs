using System;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// The service has a logic for preparing/sending a message with error handling and throttling
    /// </summary>
    public interface INotificationSender
    {
        Task<NotificationSendResult> SendNotificationAsync(Notification notification);

        Task ScheduleSendNotificationAsync(Notification notification);

        [Obsolete("need to use ScheduleSendNotificationAsync")]
        void ScheduleSendNotification(Notification notification);
    }
}
