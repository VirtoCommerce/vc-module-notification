using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsSampleModule.Web.Types
{
    public abstract class CustomerNotificationBase : EmailNotification
    {
        protected CustomerNotificationBase(string type)
            : base(type)
        {
        }

        public string RecipientLanguage { get; private set; }

        public void SetRecipientLanguage(string preferredLanguage, string fallbackLanguage)
        {
            RecipientLanguage = !string.IsNullOrEmpty(preferredLanguage) ? preferredLanguage : fallbackLanguage;

            // TODO: templates loaded from file system seem to have empty string as a LanguageCode - we might need this hack to use them
            LanguageCode = string.Empty;
        }
    }
}
