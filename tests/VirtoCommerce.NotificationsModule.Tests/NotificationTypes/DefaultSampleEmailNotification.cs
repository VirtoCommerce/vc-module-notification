using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Tests.NotificationTypes
{
    public class DefaultSampleEmailNotification : EmailNotification
    {
        public DefaultSampleEmailNotification() : base(nameof(DefaultSampleEmailNotification))
        {
        }

        public DefaultSampleEmailNotification(string type) : base(type)
        {
        }
    }
}
