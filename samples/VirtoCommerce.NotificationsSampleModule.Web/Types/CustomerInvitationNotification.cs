namespace VirtoCommerce.NotificationsSampleModule.Web.Types
{
    public class CustomerInvitationNotification : CustomerNotificationBase, ICustomerInvitationNotification
    {
        public CustomerInvitationNotification()
            : base(nameof(CustomerInvitationNotification))
        {
        }

        public string FirstName { get; set; }

        public string OutletName { get; set; }

        public string UserName { get; set; }

        public string ActivationLink { get; set; }

        public string StorefrontUrl { get; set; }
    }
}
