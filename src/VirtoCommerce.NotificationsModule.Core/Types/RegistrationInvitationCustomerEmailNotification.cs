namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class RegistrationInvitationCustomerEmailNotification : RegistrationInvitationNotificationBase
    {
        public RegistrationInvitationCustomerEmailNotification() : base(nameof(RegistrationInvitationCustomerEmailNotification))
        {
        }

        public RegistrationInvitationCustomerEmailNotification(string type) : base(type)
        {
        }
    }
}
