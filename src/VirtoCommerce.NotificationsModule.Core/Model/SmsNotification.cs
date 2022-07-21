using System;
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
        }

        protected SmsNotification(string type) : base(type)
        {
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
            var template = (SmsNotificationTemplate)Templates.FindTemplateForLanguage(message.LanguageCode);
            if (template != null)
            {
                var renderContext = new NotificationRenderContext
                {
                    Template = template.Message,
                    Language = template.LanguageCode,
                    Model = this,
                };

                smsNotificationMessage.Message = await render.RenderAsync(renderContext);
            }
            smsNotificationMessage.Number = Number;
        }

        public override void SetFromToMembers(string from, string to)
        {
            Number = to;
        }

        public override Notification PopulateFromOther(Notification other)
        {
            if (other is SmsNotification smsRequest)
            {
                Number = smsRequest.Number;
            }

            return base.PopulateFromOther(other);
        }

        #region ICloneable members

        public override object Clone()
        {
            return base.Clone() as SmsNotification;
        }

        #endregion
    }
}
