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
        private readonly TwilioSenderOptions _options;
        private readonly SmsSendingOptions _smsSendingOptions;

        public TwilioSmsNotificationMessageSender(IOptions<TwilioSenderOptions> twilioOptions, IOptions<SmsSendingOptions> smsSendingOptions)
        {
            _options = twilioOptions.Value;
            _smsSendingOptions = smsSendingOptions.Value;
        }

        public async Task SendNotificationAsync(NotificationMessage message)
        {
            TwilioClient.Init(_options.AccountId, _options.AccountPassword);

            var smsNotificationMessage = message as SmsNotificationMessage;

            if (smsNotificationMessage == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

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
    }
}
