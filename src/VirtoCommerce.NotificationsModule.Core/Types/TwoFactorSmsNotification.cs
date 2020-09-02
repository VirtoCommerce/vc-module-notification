using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class TwoFactorSmsNotification : SmsNotification
    {
        public TwoFactorSmsNotification() : base(nameof(TwoFactorSmsNotification))
        {

        }

        public TwoFactorSmsNotification(string type) : base(type)
        {
        }

        public string Token { get; set; }
    }
}
