using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    /// <summary>
    /// Sms - Kind of Notification
    /// </summary>
    public abstract class SmsNotification : Notification
    {
        [Obsolete("need to use ctor with 'type' parameter")]
        public SmsNotification()
        {
            Templates = new List<NotificationTemplate>();
        }

        public SmsNotification(string type) : base(type)
        {
            Templates = new List<NotificationTemplate>();
        }

        public override string Kind => nameof(SmsNotification);

        /// <summary>
        /// Number for sms notification
        /// </summary>
        [NotificationParameter("Recipient")]
        public string Number { get; set; }

        public override async Task ToMessageAsync(NotificationMessage message, INotificationTemplateRenderer render)
        {
            await base.ToMessageAsync(message, render);

            var smsNotificationMessage = (SmsNotificationMessage)message;
            var template = (SmsNotificationTemplate)Templates.FindWithLanguage(message.LanguageCode);
            if (template != null)
            {
                smsNotificationMessage.Message = await render.RenderAsync(template.Message, this, template.LanguageCode);
            }
            smsNotificationMessage.Number = Number;
        }

        public override void SetFromToMembers(string from, string to)
        {
            Number = to;
        }

        public override Notification PopulateFromRequest(Notification request)
        {
            if (request is SmsNotification smsRequest)
            {
                Number = smsRequest.Number;
            }

            return base.PopulateFromRequest(request);
        }

        #region ICloneable members

        public override object Clone()
        {
            return base.Clone() as SmsNotification;
        }

        #endregion
    }
}
