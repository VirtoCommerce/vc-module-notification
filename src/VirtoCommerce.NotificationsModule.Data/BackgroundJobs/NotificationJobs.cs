using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Security.Events;

namespace VirtoCommerce.NotificationsModule.Data.BackgroundJobs;

public class SendResetPasswordNotificationPayload
{
    public UserRequestPasswordResetEvent Event { get; set; }
}

/// <summary>
/// Engine-agnostic replacement for the former Hangfire
/// <c>BackgroundJob.Enqueue(() =&gt; TryToSendNotificationsAsync(message))</c>.
/// </summary>
public class SendResetPasswordNotificationJob(
    INotificationSearchService notificationSearchService,
    INotificationSender notificationSender,
    IOptions<EmailSendingOptions> emailSendingOptions) : IBackgroundJobHandler<SendResetPasswordNotificationPayload>
{
    public async Task Execute(SendResetPasswordNotificationPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var argument = payload.Event;

        var notification = await notificationSearchService.GetNotificationAsync<ResetPasswordEmailNotification>();
        notification.Url = argument.CallbackUrl;
        notification.From = emailSendingOptions.Value.DefaultSender;
        notification.To = argument.User.Email;

        await notificationSender.ScheduleSendNotificationAsync(notification);
    }
}

public class SendNotificationMessagePayload
{
    public string MessageId { get; set; }
}

/// <summary>
/// Engine-agnostic replacement for the former Hangfire
/// <c>IBackgroundJobClient.Enqueue(() =&gt; TrySendNotificationMessageAsync(messageId))</c>.
/// </summary>
public class SendNotificationMessageJob(INotificationSender notificationSender)
    : IBackgroundJobHandler<SendNotificationMessagePayload>
{
    public Task Execute(SendNotificationMessagePayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        => notificationSender.TrySendNotificationMessageAsync(payload.MessageId);
}
