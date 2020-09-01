using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class ResetPasswordEmailNotification : EmailNotification
    {
        public ResetPasswordEmailNotification() : base(nameof(ResetPasswordEmailNotification))
        {

        }

        public string Url { get; set; }
    }
}
