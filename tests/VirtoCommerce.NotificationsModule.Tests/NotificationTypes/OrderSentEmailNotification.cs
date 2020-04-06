using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Tests.Model;

namespace VirtoCommerce.NotificationsModule.Tests.NotificationTypes
{
    public class OrderSentEmailNotification : EmailNotification
    {
        public OrderSentEmailNotification() : base(nameof(OrderSentEmailNotification))
        {

        }

        public CustomerOrder CustomerOrder { get; set; }
    }
}
