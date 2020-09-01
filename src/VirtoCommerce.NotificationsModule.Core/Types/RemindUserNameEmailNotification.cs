using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class RemindUserNameEmailNotification : EmailNotification
    {
        public RemindUserNameEmailNotification() : base(nameof(RemindUserNameEmailNotification))
        {
            //for backward compatibility v.2
            Alias = "RemindUserNameNotification";
        }

        public string UserName { get; set; }
    }
}
