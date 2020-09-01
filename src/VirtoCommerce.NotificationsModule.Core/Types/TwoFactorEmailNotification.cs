using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.Notifications.Core.Types
{
    public class TwoFactorEmailNotification : EmailNotification
    {
        public TwoFactorEmailNotification() : base(nameof(TwoFactorEmailNotification))
        {

        }
    }
}
