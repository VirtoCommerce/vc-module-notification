using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Notifications;

namespace VirtoCommerce.NotificationsModule.Data.Senders
{
    [Obsolete("Use notification feature instead")]
    public class EmailNotificationMessageSender : IEmailSender
    {
        private readonly INotificationMessageSenderFactory _notificationMessageSenderProviderFactory;
        private readonly EmailSendingOptions _emailSendingOptions;

        public EmailNotificationMessageSender(
            INotificationMessageSenderFactory notificationMessageSenderProviderFactory
            , IOptions<EmailSendingOptions> emailSendingOptions)
        {
            _notificationMessageSenderProviderFactory = notificationMessageSenderProviderFactory;
            _emailSendingOptions = emailSendingOptions.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string body)
        {
            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance(nameof(EmailNotificationMessage));
            if (message is EmailNotificationMessage notificationMessage)
            {
                notificationMessage.From = _emailSendingOptions.DefaultSender;
                notificationMessage.To = email;
                notificationMessage.Subject = subject;
                notificationMessage.Body = body;
            }

            await _notificationMessageSenderProviderFactory.GetSender(message).SendNotificationAsync(message);
        }


    }
}
