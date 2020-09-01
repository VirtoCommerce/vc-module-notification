using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Types
{
    public class RegistrationEmailNotification : EmailNotification
    {
        public RegistrationEmailNotification() : base(nameof(RegistrationEmailNotification))
        {

        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Login { get; set; }
    }
}
