using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Extensions
{
    public static class NotificationMessageExtensions
    {
        public static bool CanSend(this NotificationMessage message, NotificationSendResult notificationSendResult)
        {
            if (message.Status == NotificationMessageStatus.Error)
            {
                notificationSendResult.ErrorMessage = $"Can't send notification message by {message.Id}. There are errors.";
                return false;
            }

            if (message.Status == NotificationMessageStatus.Sent)
            {
                notificationSendResult.ErrorMessage = $"Notification message by {message.Id} already sent";
                return false;
            }

            return true;
        }
    }
}
