using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Security.Events;

namespace VirtoCommerce.NotificationsModule.Data.Handlers
{
    public class RequestPasswordResetHandler : IEventHandler<UserRequestPasswordResetEvent>
    {
        private readonly INotificationSender _notificationSender;
        private readonly INotificationSearchService _notificationSearchService;
        private readonly EmailSendingOptions _emailSendingOptions;

        public RequestPasswordResetHandler(INotificationSender notificationSender,
            INotificationSearchService notificationSearchService,
            IOptions<EmailSendingOptions> emailSendingOptions)
        {
            _notificationSender = notificationSender;
            _notificationSearchService = notificationSearchService;
            _emailSendingOptions = emailSendingOptions.Value;
        }

        public Task Handle(UserRequestPasswordResetEvent message)
        {
            BackgroundJob.Enqueue(() => TryToSendNotificationsAsync(message));
            return Task.CompletedTask;
        }

        public async Task TryToSendNotificationsAsync(UserRequestPasswordResetEvent argument)
        {
            var notification = await _notificationSearchService.GetNotificationAsync<ResetPasswordEmailNotification>();
            notification.Url = argument.CallbackUrl;
            notification.From = _emailSendingOptions.DefaultSender;
            notification.To = argument.User.Email;
            await _notificationSender.ScheduleSendNotificationAsync(notification);
        }
    }
}
