using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.SendGrid
{
    public class SendGridEmailNotificationMessageSender : INotificationMessageSender
    {
        private readonly SendGridSenderOptions _emailSendingOptions;

        public SendGridEmailNotificationMessageSender(IOptions<SendGridSenderOptions> emailSendingOptions)
        {
            _emailSendingOptions = emailSendingOptions.Value;
        }

        public async Task SendNotificationAsync(NotificationMessage message)
        {
            try
            {
                var emailNotificationMessage = message as EmailNotificationMessage;

                if (emailNotificationMessage == null)
                {
                    throw new ArgumentNullException(nameof(emailNotificationMessage));
                }

                var fromAddress = new EmailAddress(emailNotificationMessage.From);
                var toAddress = new EmailAddress(emailNotificationMessage.To);

                var client = new SendGridClient(_emailSendingOptions.ApiKey);
                var mailMsg = new SendGridMessage
                {
                    From = fromAddress,
                    Subject = emailNotificationMessage.Subject,
                    HtmlContent = emailNotificationMessage.Body,
                };

                mailMsg.AddTo(toAddress);

                if (!emailNotificationMessage.CC.IsNullOrEmpty())
                {
                    foreach (var ccEmail in emailNotificationMessage.CC)
                    {
                        mailMsg.AddCc(ccEmail);
                    }
                }
                if (!emailNotificationMessage.BCC.IsNullOrEmpty())
                {
                    foreach (var bccEmail in emailNotificationMessage.BCC)
                    {
                        mailMsg.AddBcc(bccEmail);
                    }
                }

                var response = await client.SendEmailAsync(mailMsg);

                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    throw new SentNotificationException(errorBody);
                }
            }
            catch (SmtpException ex)
            {
                throw new SentNotificationException(ex);
            }

        }
    }
}
