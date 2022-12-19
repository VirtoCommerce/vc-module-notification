using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    /// <summary>
    /// Template of email notification
    /// </summary>
    public class EmailNotificationTemplate : NotificationTemplate
    {
        public override string Kind => nameof(EmailNotification);

        /// <summary>
        /// Subject of Notification
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Body of Notification
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Sample notification object data, serialized to json.
        /// This used to preview the full notification while edit.
        /// </summary>
        public string Sample { get; set; }

        public string NotificationLayoutId { get; set; }

        public override void PopulateFromLocalizedContent(LocalizedTemplateContent content)
        {
            base.PopulateFromLocalizedContent(content);

            if (content.PartName.EqualsInvariant(nameof(Subject)))
            {
                Subject = content.Content;
            }
            else if (content.PartName.EqualsInvariant(nameof(Body)))
            {
                Body = content.Content;
            }
            else
            {
                Sample = content.Content;
            }
        }
    }
}
