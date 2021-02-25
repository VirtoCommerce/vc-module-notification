using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    /// <summary>
    /// Account email address verification notification
    /// The sent link will change verification status for an account email address
    /// </summary>
    public class VerificationEmailNotification : EmailNotification
    {
        public VerificationEmailNotification() : base(nameof(VerificationEmailNotification))
        {
        }

        public VerificationEmailNotification(string type) : base(type)
        {
        }

        public string Url { get; set; }
    }
}
