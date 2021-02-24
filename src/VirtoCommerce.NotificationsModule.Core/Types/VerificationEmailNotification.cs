using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class VerificationEmailNotification : EmailNotification
    {
        public VerificationEmailNotification() : base(nameof(VerificationEmailNotification))
        {
            //for backward compatibility v.2
            Alias = "EmailVerificationNotification";
        }

        public VerificationEmailNotification(string type) : base(type)
        {

        }

        public string Url { get; set; }

    }

}
