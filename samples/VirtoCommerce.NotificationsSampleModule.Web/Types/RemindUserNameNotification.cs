using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsSampleModule.Web.Types
{
    public class RemindUserNameNotification : EmailNotification
    {
        public RemindUserNameNotification() : base(nameof(RemindUserNameNotification))
        {

        }
    }
}
