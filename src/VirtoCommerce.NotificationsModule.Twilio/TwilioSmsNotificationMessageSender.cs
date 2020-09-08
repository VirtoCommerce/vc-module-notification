using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.Twilio
{
    public class TwilioSmsNotificationMessageSender : INotificationMessageSender
    {
        public const string Name = "Twilio";
        private readonly TwilioSenderOptions _options;
        private readonly SmsSendingOptions _smsSendingOptions;

        public TwilioSmsNotificationMessageSender(IOptions<TwilioSenderOptions> twilioOptions, IOptions<SmsSendingOptions> smsSendingOptions)
        {
            _options = twilioOptions.Value;
            _smsSendingOptions = smsSendingOptions.Value;
        }

        public virtual bool CanSend(NotificationMessage message) => message is SmsNotificationMessage;

        public async Task SendNotificationAsync(NotificationMessage message)
        {
            ThrowIfSendingNotPossible(message);

            TwilioClient.Init(_options.AccountId, _options.AccountPassword);

            var smsNotificationMessage = message as SmsNotificationMessage;

            try
            {
                await MessageResource.CreateAsync(
                    body: smsNotificationMessage.Message,
                    from: new PhoneNumber(_smsSendingOptions.SmsDefaultSender),
                    to: smsNotificationMessage.Number
                );
            }
            catch (ApiException ex)
            {
                throw new SentNotificationException(ex);
            }
        }

        protected virtual void ThrowIfSendingNotPossible(NotificationMessage message)
        {
            if (!CanSend(message))
            {
                throw new ArgumentNullException(nameof(message));
            }
        }
    }
}
