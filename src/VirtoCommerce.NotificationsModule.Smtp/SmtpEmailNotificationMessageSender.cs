using System;
using System.IO;
using System.Net.Http;
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
    private readonly IHttpClientFactory _httpClientFactory;

    public SmtpEmailNotificationMessageSender(IOptions<SmtpSenderOptions> smtpOptions,
        IOptions<EmailSendingOptions> emailSendingOptions,
        IHttpClientFactory httpClientFactory)
    {
        _smtpOptions = smtpOptions.Value;
        _emailSendingOptions = emailSendingOptions.Value;
        _httpClientFactory = httpClientFactory;
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

            mailMsg.From.Add(MailboxAddress.Parse(emailNotificationMessage.From ?? _emailSendingOptions.DefaultSender));
            mailMsg.To.Add(MailboxAddress.Parse(emailNotificationMessage.To));

            if (!string.IsNullOrEmpty(emailNotificationMessage.ReplyTo) &&
                MailboxAddress.TryParse(emailNotificationMessage.ReplyTo, out var replyToAddress))
            {
                mailMsg.ReplyTo.Add(replyToAddress);
            }
            else if (!string.IsNullOrEmpty(_emailSendingOptions?.DefaultReplyTo) &&
                MailboxAddress.TryParse(_emailSendingOptions.DefaultReplyTo, out var defaultReplyToAddress))
            {
                mailMsg.ReplyTo.Add(defaultReplyToAddress);
            }

            if (!emailNotificationMessage.CC.IsNullOrEmpty())
            {
                foreach (var ccEmail in emailNotificationMessage.CC)
                {
                    if (MailboxAddress.TryParse(ccEmail, out var address))
                    {
                        mailMsg.Cc.Add(address);
                    }
                }
            }

            if (!emailNotificationMessage.BCC.IsNullOrEmpty())
            {
                foreach (var bccEmail in emailNotificationMessage.BCC)
                {
                    if (MailboxAddress.TryParse(bccEmail, out var address))
                    {
                        mailMsg.Bcc.Add(address);
                    }
                }
            }

            mailMsg.Subject = emailNotificationMessage.Subject;

            var builder = new BodyBuilder { HtmlBody = emailNotificationMessage.Body };

            foreach (var attachment in emailNotificationMessage.Attachments)
            {
                if (!string.IsNullOrEmpty(attachment.Url))
                {
                    if (Uri.TryCreate(attachment.Url, UriKind.Absolute, out var uri))
                    {
                        // Absolute URL: Download file asynchronously
                        var httpClient = _httpClientFactory.CreateClient();
                        await using var stream = await httpClient.GetStreamAsync(attachment.Url);
                        await builder.Attachments.AddAsync(attachment.FileName, stream);
                    }
                    else
                    {
                        // Local file: Open file stream
                        await using var stream = File.OpenRead(attachment.Url);
                        await builder.Attachments.AddAsync(attachment.FileName, stream);
                    }
                }
            }

            mailMsg.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpOptions.SmtpServer, _smtpOptions.Port, _smtpOptions.ForceSslTls
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable);
            await client.AuthenticateAsync(_smtpOptions.Login, _smtpOptions.Password);
            await client.SendAsync(mailMsg);
            await client.DisconnectAsync(quit: true);
        }
        catch (Exception ex)
        {
            throw new SentNotificationException(ex);
        }
    }
}
