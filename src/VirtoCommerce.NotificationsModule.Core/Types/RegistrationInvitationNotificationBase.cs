using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public abstract class RegistrationInvitationNotificationBase : EmailNotification
    {
        protected RegistrationInvitationNotificationBase(string type) : base(type)
        {
        }

        public string InviteUrl { get; set; }
        public string Message { get; set; }
    }
}
