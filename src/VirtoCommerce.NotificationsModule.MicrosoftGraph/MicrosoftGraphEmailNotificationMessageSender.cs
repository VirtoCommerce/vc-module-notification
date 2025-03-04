using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.MicrosoftGraph;

public class MicrosoftGraphEmailNotificationMessageSender(
    IOptions<MicrosoftGraphSenderOptions> microsoftGraphOptions,
    IOptions<EmailSendingOptions> emailSendingOptions,
    IEmailAttachmentService attachmentService)
    : INotificationMessageSender
{
    public const string Name = "MicrosoftGraph";

    private readonly MicrosoftGraphSenderOptions _microsoftGraphOptions = microsoftGraphOptions.Value;
    private readonly EmailSendingOptions _emailSendingOptions = emailSendingOptions.Value;

    public virtual bool CanSend(NotificationMessage message) => message is EmailNotificationMessage;

    public async Task SendNotificationAsync(NotificationMessage message)
    {
        var emailNotificationMessage = message as EmailNotificationMessage
            ?? throw new ArgumentException($"The message is not {nameof(EmailNotificationMessage)} type");

        emailNotificationMessage.From ??= _emailSendingOptions.DefaultSender;

        try
        {
            var graphMessage = new Message
            {
                Subject = emailNotificationMessage.Subject,
                Body = new ItemBody { ContentType = BodyType.Html, Content = emailNotificationMessage.Body },
                From = NewRecipient(emailNotificationMessage.From),
                ToRecipients = [NewRecipient(emailNotificationMessage.To)],
                CcRecipients = emailNotificationMessage.CC?.Select(NewRecipient).ToList() ?? [],
                BccRecipients = emailNotificationMessage.BCC?.Select(NewRecipient).ToList() ?? []
            };

            if (!emailNotificationMessage.Attachments.IsNullOrEmpty())
            {
                graphMessage.Attachments ??= [];
                foreach (var attachment in emailNotificationMessage.Attachments)
                {
                    await using var stream = await attachmentService.GetStreamAsync(attachment);

                    graphMessage.Attachments.Add(new FileAttachment
                    {
                        ContentType = attachment.MimeType,
                        Name = attachment.FileName,
                        Size = Convert.ToInt32(attachment.Size),
                        ContentBytes = await stream.ReadAllBytesAsync(),
                    });
                }
            }

            var request = new SendMailPostRequestBody { SaveToSentItems = false, Message = graphMessage };

            var options = new TokenCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud };
            var clientSecretCredential = new ClientSecretCredential(
                _microsoftGraphOptions.TenantId,
                _microsoftGraphOptions.ApplicationId,
                _microsoftGraphOptions.SecretValue,
                options
            );
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            var microsoftGraphClient = new GraphServiceClient(clientSecretCredential, scopes);
            await microsoftGraphClient.Users[emailNotificationMessage.From].SendMail.PostAsync(request);
        }
        catch (Exception ex)
        {
            throw new SentNotificationException(ex);
        }
    }

    private static Recipient NewRecipient(string emailAddress) => new()
    {
        EmailAddress = new EmailAddress { Address = emailAddress }
    };
}
