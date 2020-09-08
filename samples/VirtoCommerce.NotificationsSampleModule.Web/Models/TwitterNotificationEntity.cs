using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsSampleModule.Web.Models
{
    public class TwitterNotificationEntity : NotificationEntity
    {
        public string Post { get; set; }

        public override Notification ToModel(Notification notification)
        {
            var result = base.ToModel(notification);
            if (result is TwitterNotification twitterNotification)
            {
                twitterNotification.Post = Post;
            }
            return result;
        }

        public override NotificationEntity FromModel(Notification notification, PrimaryKeyResolvingMap pkMap)
        {
            if (notification is TwitterNotification twitterNotification)
            {
                Post = twitterNotification.Post;
            }
            return base.FromModel(notification, pkMap);
        }

        public override void Patch(NotificationEntity notification)
        {
            if (notification is TwitterNotificationEntity entity)
            {
                entity.Post = Post;
            }
            base.Patch(notification);
        }
    }
}
