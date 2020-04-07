using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Tests.NotificationTypes
{
    public class SampleNotification : EmailNotification
    {
        public SampleNotification() : base(nameof(SampleNotification))
        {

        }
    }
}
