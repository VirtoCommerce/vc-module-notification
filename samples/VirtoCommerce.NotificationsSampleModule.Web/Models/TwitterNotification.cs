using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsSampleModule.Web.Models
{
    public abstract class TwitterNotification : Notification
    {
        protected TwitterNotification(string type) : base(type)
        {
            Templates = new List<NotificationTemplate>();
        }

        public string Post { get; set; }
        public override string Kind => nameof(TwitterNotification);

        public override void SetFromToMembers(string from, string to)
        {
        }
    }
}
