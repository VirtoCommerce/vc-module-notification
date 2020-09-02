using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class ChangePhoneNumberSmsNotification : SmsNotification
    {
        public ChangePhoneNumberSmsNotification() : base(nameof(ChangePhoneNumberSmsNotification))
        {

        }

        public ChangePhoneNumberSmsNotification(string type) : base(type)
        {

        }

        public string Token { get; set; }
    }
}
