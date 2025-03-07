using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.SendGrid
{
    public class SendGridEmailNotificationMessageSender : INotificationMessageSender
    {
        public const string Name = "SendGrid";
        private readonly SendGridSenderOptions _sendGridOptions;
        private readonly EmailSendingOptions _emailSendingOptions;
        private readonly IEmailAttachmentService _attachmentService;

        public SendGridEmailNotificationMessageSender(
            IOptions<SendGridSenderOptions> sendGridOptions,
            IOptions<EmailSendingOptions> emailSendingOptions,
            IEmailAttachmentService attachmentService)
        {
            _sendGridOptions = sendGridOptions.Value;
            _emailSendingOptions = emailSendingOptions.Value;
            _attachmentService = attachmentService;
        }

        public virtual bool CanSend(NotificationMessage message) => message is EmailNotificationMessage;

        public virtual async Task SendNotificationAsync(NotificationMessage message)
        {
            if (!CanSend(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            var emailNotificationMessage = message as EmailNotificationMessage;

            var mailMsg = await ToSendGridMessageAsync(emailNotificationMessage);

            try
            {
                var client = new SendGridClient(_sendGridOptions.ApiKey);

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

        protected virtual async Task<SendGridMessage> ToSendGridMessageAsync(EmailNotificationMessage emailNotificationMessage)
        {
            var fromAddress = new EmailAddress(emailNotificationMessage.From ?? _emailSendingOptions.DefaultSender);
            var toAddress = new EmailAddress(emailNotificationMessage.To);

            var mailMsg = new SendGridMessage
            {
                From = fromAddress,
                Subject = emailNotificationMessage.Subject,
                HtmlContent = emailNotificationMessage.Body,
            };

            var replyTo = emailNotificationMessage.ReplyTo ?? _emailSendingOptions.DefaultReplyTo;
            if (!string.IsNullOrEmpty(replyTo))
            {
                mailMsg.ReplyTo = new EmailAddress(replyTo);
            }

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

            foreach (var attachment in emailNotificationMessage.Attachments ?? [])
            {
                await using var stream = await _attachmentService.GetStreamAsync(attachment);
                var fileBytes = await stream.ReadAllBytesAsync();
                var base64Content = Convert.ToBase64String(fileBytes);

                mailMsg.AddAttachment(
                    attachment.FileName,
                    base64Content,
                    attachment.MimeType);
            }

            return mailMsg;
        }
    }
}
