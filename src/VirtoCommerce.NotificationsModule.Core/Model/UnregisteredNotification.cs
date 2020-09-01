using System;
using System.Collections.Generic;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    //The special type for handle cases when  the system have stored the notification objects with unregistered types. 
    public class UnregisteredNotification : Notification
    {
        public UnregisteredNotification() : base(nameof(UnregisteredNotification))
        {
            Templates = new List<NotificationTemplate>();
        }
        public override string Kind => "undef";
        

        public override void SetFromToMembers(string from, string to)
        {
            throw new NotImplementedException();
        }
    }
}
