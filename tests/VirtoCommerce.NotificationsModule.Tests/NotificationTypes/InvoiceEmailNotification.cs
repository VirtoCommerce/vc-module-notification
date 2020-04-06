using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Tests.Model;

namespace VirtoCommerce.NotificationsModule.Tests.NotificationTypes
{
    public class InvoiceEmailNotification : EmailNotification
    {
        public InvoiceEmailNotification() : base(nameof(InvoiceEmailNotification))
        {

        }

        public CustomerOrder CustomerOrder { get; set; }
    }
}
