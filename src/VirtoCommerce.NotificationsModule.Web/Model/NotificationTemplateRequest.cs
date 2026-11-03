using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Web.Model
{
    public class NotificationTemplateRequest
    {
        public string Text { get; set; }
        public string NotificationLayoutId { get; set; }
        public Notification Data { get; set; }

        /// <summary>
        /// Layout text to render the template in, for a preview of a layout that is not saved yet.
        /// Takes precedence over <see cref="NotificationLayoutId"/>.
        /// </summary>
        public string LayoutTemplate { get; set; }

        /// <summary>
        /// Store whose email brand is used in the preview. Without it the brand of the notification's store, or the global brand.
        /// </summary>
        public string StoreId { get; set; }
    }
}
