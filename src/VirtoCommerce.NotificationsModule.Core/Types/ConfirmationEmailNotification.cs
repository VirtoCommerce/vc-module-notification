using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class ConfirmationEmailNotification : EmailNotification
    {
        public ConfirmationEmailNotification()
        {
            //for backward compatibility v.2
            Alias = "EmailConfirmationNotification";
        }

        public string Url { get; set; }
    }
}
