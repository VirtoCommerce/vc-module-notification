using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class ConfirmationEmailNotification : EmailNotification
    {
        public ConfirmationEmailNotification()
        {
            Alias = "EmailConfirmationNotification";
        }

        public string Url { get; set; }
    }
}
