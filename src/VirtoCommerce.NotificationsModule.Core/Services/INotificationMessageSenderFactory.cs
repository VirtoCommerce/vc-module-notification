using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    public interface INotificationMessageSenderFactory
    {
        INotificationMessageSender GetSender(NotificationMessage message);
    }
}
