using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class RemindUserNameEmailNotification : EmailNotification
    {
        public RemindUserNameEmailNotification()
        {
            //for backward compatibility v.2
            Alias = "RemindUserNameNotification";
        }
    }
}
