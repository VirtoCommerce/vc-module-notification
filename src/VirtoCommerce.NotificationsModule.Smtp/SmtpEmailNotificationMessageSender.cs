using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Smtp;

public class SmtpEmailNotificationMessageSender : INotificationMessageSender
{
    public const string Name = "Smtp";
    private readonly SmtpSenderOptions _smtpOptions;
    private readonly EmailSendingOptions _emailSendingOptions;

    public SmtpEmailNotificationMessageSender(IOptions<SmtpSenderOptions> smtpOptions, IOptions<EmailSendingOptions> emailSendingOptions)
    {
        _smtpOptions = smtpOptions.Value;
        _emailSendingOptions = emailSendingOptions.Value;
    }

    public virtual bool CanSend(NotificationMessage message)
    {
        return message is EmailNotificationMessage;
    }

    public async Task SendNotificationAsync(NotificationMessage message)
    {
        var emailNotificationMessage = message as EmailNotificationMessage ?? throw new ArgumentException($"The message is not {nameof(EmailNotificationMessage)} type");

        try
        {
            using var mailMsg = new MimeMessage();
                
            mailMsg.From.Add(new MailboxAddress(null, emailNotificationMessage.From ?? _emailSendingOptions.DefaultSender));
            mailMsg.To.Add(new MailboxAddress(null, emailNotificationMessage.To));

            if (!emailNotificationMessage.CC.IsNullOrEmpty())
            {
                foreach (var ccEmail in emailNotificationMessage.CC)
                {
                    mailMsg.Cc.Add(new MailboxAddress(null, ccEmail));
                }
            }

            if (!emailNotificationMessage.BCC.IsNullOrEmpty())
            {
                foreach (var bccEmail in emailNotificationMessage.BCC)
                {
                    mailMsg.Bcc.Add(new MailboxAddress(null, bccEmail));
                }
            }

            mailMsg.Subject = emailNotificationMessage.Subject;
                    
            var bodyBuilder = new BodyBuilder { HtmlBody = emailNotificationMessage.Body };
                
            foreach (var attachment in emailNotificationMessage.Attachments)
            {
                await bodyBuilder.Attachments.AddAsync(attachment.FileName, ContentType.Parse(attachment.MimeType));
            }

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpOptions.SmtpServer, _smtpOptions.Port, _smtpOptions.EnableSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable);
            await client.AuthenticateAsync(_smtpOptions.Login, _smtpOptions.Password);
            await client.SendAsync(mailMsg);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            throw new SentNotificationException(ex);
        }
    }
}
