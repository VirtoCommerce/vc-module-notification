namespace VirtoCommerce.NotificationsSampleModule.Web.Types
{
    public interface ICustomerInvitationNotification
    {
        string FirstName { get; set; }

        string OutletName { get; set; }

        string UserName { get; set; }

        string ActivationLink { get; set; }

        string StorefrontUrl { get; set; }

        void SetRecipientLanguage(string preferredLanguage, string fallbackLanguage);
    }
}
