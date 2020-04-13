using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    /// <summary>
    /// Type of Notification for the Email
    /// </summary>
    public abstract class EmailNotification : Notification
    {
        [Obsolete("need to use ctor with 'type' parameter")]
        public EmailNotification()
        {
            Templates = new List<NotificationTemplate>();
            Attachments = new List<EmailAttachment>();
        }

        public EmailNotification(string type) : base(type)
        {
            Templates = new List<NotificationTemplate>();
            Attachments = new List<EmailAttachment>();
        }

        public override string Kind => nameof(EmailNotification);
        /// <summary>
        /// Sender
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Recipient
        /// </summary>
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

        public override async Task ToMessageAsync(NotificationMessage message, INotificationTemplateRenderer render)
        {
            await base.ToMessageAsync(message, render);

            var emailMessage = (EmailNotificationMessage)message;

            var template = (EmailNotificationTemplate)Templates.FindWithLanguage(message.LanguageCode);
            if (template != null)
            {
                emailMessage.Subject = await render.RenderAsync(template.Subject, this, template.LanguageCode);
                emailMessage.Body = await render.RenderAsync(template.Body, this, template.LanguageCode);
            }

            emailMessage.From = From;
            emailMessage.To = To;
            emailMessage.CC = CC;
            emailMessage.BCC = BCC;
            emailMessage.Attachments = Attachments;
        }

        public override void SetFromToMembers(string from, string to)
        {
            From = from;
            To = to;
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
