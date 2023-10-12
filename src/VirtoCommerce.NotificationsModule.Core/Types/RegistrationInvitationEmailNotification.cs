namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class RegistrationInvitationEmailNotification : RegistrationInvitationNotificationBase
    {
        public RegistrationInvitationEmailNotification() : base(nameof(RegistrationInvitationEmailNotification))
        {
            //for backward compatibility v.2
            Alias = "RegistrationInvitationNotification";
        }

        public RegistrationInvitationEmailNotification(string type) : base(type)
        {

        }
    }
}
