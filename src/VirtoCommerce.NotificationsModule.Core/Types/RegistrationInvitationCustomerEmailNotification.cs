using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class RegistrationInvitationCustomerEmailNotification : EmailNotification
    {
        public RegistrationInvitationCustomerEmailNotification() : base(nameof(RegistrationInvitationCustomerEmailNotification))
        {
        }

        public RegistrationInvitationCustomerEmailNotification(string type) : base(type)
        {
        }

        public string InviteUrl { get; set; }
        public string Message { get; set; }
    }
}
