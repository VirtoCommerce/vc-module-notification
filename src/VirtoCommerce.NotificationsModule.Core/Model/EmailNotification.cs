using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    /// <summary>
    /// Type of Notification for the Email
    /// </summary>
    public abstract class EmailNotification : Notification, IHasNotificationLayoutId
    {
        [Obsolete("need to use ctor with 'type' parameter")]
        public EmailNotification()
        {
            Attachments = new List<EmailAttachment>();
        }

        protected EmailNotification(string type) : base(type)
        {
            Attachments = new List<EmailAttachment>();
        }

        public override string Kind => nameof(EmailNotification);

        /// <summary>
        /// Sender
        /// </summary>
        [NotificationParameter("Sender")]
        public string From { get; set; }

        /// <summary>
        /// Recipient
        /// </summary>
        [NotificationParameter("Recipient")]
        public string To { get; set; }

        /// <summary>
        /// Array of CC recipients
        /// </summary>
        public string[] CC { get; set; }

        /// <summary>
        /// Array of BCC recipients
        /// </summary>
        public string[] BCC { get; set; }

        public IList<EmailAttachment> Attachments { get; set; }

        public string NotificationLayoutId { get; set; }

        public override async Task ToMessageAsync(NotificationMessage message, INotificationTemplateRenderer render)
        {
            await base.ToMessageAsync(message, render);

            var emailMessage = (EmailNotificationMessage)message;

            var template = (EmailNotificationTemplate)Templates.FindTemplateForLanguage(message.LanguageCode);
            if (template != null)
            {
                //add layout id to notification object clone, this will force layout rendering for body 
                var notification = Clone() as EmailNotification;

                notification.NotificationLayoutId = null;
                emailMessage.Subject = await render.RenderAsync(template.Subject, notification, template.LanguageCode);

                notification.NotificationLayoutId = template.NotificationLayoutId;
                emailMessage.Body = await render.RenderAsync(template.Body, notification, template.LanguageCode);
            }

            emailMessage.From = From;
            emailMessage.To = To;
            emailMessage.CC = CC;
            emailMessage.BCC = BCC;
            emailMessage.Attachments = Attachments;
        }

        public override void ReduceDetails(string responseGroup)
        {
            //Reduce details according to response group
            var notificationResponseGroup = EnumUtility.SafeParseFlags(responseGroup, NotificationResponseGroup.Full);

            if (!notificationResponseGroup.HasFlag(NotificationResponseGroup.WithAttachments))
            {
                Attachments = null;
            }
            base.ReduceDetails(responseGroup);
        }

        public override void SetFromToMembers(string from, string to)
        {
            From = from;
            To = to;
        }

        public override Notification PopulateFromOther(Notification other)
        {
            if (other is EmailNotification emailRequest)
            {
                From = emailRequest.From;
                To = emailRequest.To;
                CC = emailRequest.CC;
                BCC = emailRequest.BCC;
                Attachments = emailRequest.Attachments;
            }

            return base.PopulateFromOther(other);
        }

        #region ICloneable members

        public override object Clone()
        {
            var result = base.Clone() as EmailNotification;

            result.Attachments = Attachments?.Select(x => x.Clone()).OfType<EmailAttachment>().ToList();

            return result;
        }

        #endregion
    }
}
