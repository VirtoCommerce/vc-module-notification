using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class ResetPasswordSmsNotification : SmsNotification
    {
        public ResetPasswordSmsNotification() : base(nameof(ResetPasswordSmsNotification))
        {

        }

        public ResetPasswordSmsNotification(string type) : base(type)
        {
        }

        public string Token { get; set; }
    }
}
