using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class RegistrationInvitationEmailNotification : EmailNotification
    {
        public RegistrationInvitationEmailNotification() : base(nameof(RegistrationInvitationEmailNotification))
        {
            //for backward compatibility v.2
            Alias = "RegistrationInvitationNotification";
        }

        public string InviteUrl { get; set; }
        public string Message { get; set; }
    }
}
