using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsSampleModule.Web.Types
{
    public class SampleEmailNotification : EmailNotification
    {
        public SampleEmailNotification() : base(nameof(SampleEmailNotification))
        {
        }

        public SampleEmailNotification(string type) : base(type)
        {
        }
    }
}
