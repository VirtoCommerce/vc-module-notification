using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Tests.NotificationTypes
{
    public class SampleEmailNotification : EmailNotification
    {
        public SampleEmailNotification() : base(nameof(SampleEmailNotification))
        {

        }
    }
}
