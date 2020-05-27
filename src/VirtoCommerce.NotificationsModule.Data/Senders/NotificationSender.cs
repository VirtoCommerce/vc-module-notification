using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Polly;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Senders
{
    public class NotificationSender : INotificationSender
    {
        private readonly int _maxRetryAttempts = 1;
        private readonly INotificationTemplateRenderer _notificationTemplateRender;
        private readonly INotificationMessageService _notificationMessageService;
        private readonly INotificationMessageSenderProviderFactory _notificationMessageAccessor;
        private readonly ILogger<NotificationSender> _logger;
        private readonly IBackgroundJobClient _jobClient;

        public NotificationSender(INotificationTemplateRenderer notificationTemplateRender
            , INotificationMessageService notificationMessageService
            , ILogger<NotificationSender> logger
            , INotificationMessageSenderProviderFactory notificationMessageAccessor
            , IBackgroundJobClient jobClient)
        {
            _notificationTemplateRender = notificationTemplateRender;
            _notificationMessageService = notificationMessageService;
            _notificationMessageAccessor = notificationMessageAccessor;
            _logger = logger;
            _jobClient = jobClient;
        }

        public async Task ScheduleSendNotificationAsync(Notification notification)
        {
            if (notification.IsActive)
            {
                //Important! need to except 'predefined/hardcoded' notifications
                //because after deserialization in 'SendNotificationAsync' method of the job
                //generated couple 'predefined' templates
                //in the result the notification has couple templates with same content
                if (notification.Templates.Any(x => x.IsPredefined))
                {
                    notification.Templates = notification.Templates.Where(x => !x.IsPredefined).ToList();
                }

                var message = await CreateMessageAsync(notification);

                _jobClient.Enqueue(() => TrySendNotificationMessageAsync(message.Id));
            }
        }

        [Obsolete("need to use ScheduleSendNotificationAsync")]
        public void ScheduleSendNotification(Notification notification)
        {
            ScheduleSendNotificationAsync(notification).GetAwaiter().GetResult();
        }

        

        public async Task<NotificationSendResult> SendNotificationAsync(Notification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (notification.IsActive)
            {
                var message = await CreateMessageAsync(notification);

                return await TrySendNotificationMessageAsync(message.Id);
            }

            return new NotificationSendResult();
        }


        public async Task<NotificationSendResult> TrySendNotificationMessageAsync(string messageId)
        {
            var result = new NotificationSendResult();

            var message = (await _notificationMessageService.GetNotificationsMessageByIds(new[] {messageId})).FirstOrDefault();

            if (message == null)
            {
                result.ErrorMessage = $"can't find notification message by {messageId}";
                return result;
            }

            var policy = Policy.Handle<SentNotificationException>().WaitAndRetryAsync(_maxRetryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt))
                , (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogError(exception, $"Retry {retryCount} of {context.PolicyKey}, due to: {exception}.");
                });

            var policyResult = await policy.ExecuteAndCaptureAsync(() =>
            {
                message.LastSendAttemptDate = DateTime.Now;
                message.SendAttemptCount++;
                return _notificationMessageAccessor.GetSenderForNotificationType(message.Kind).SendNotificationAsync(message);
            });

            if (policyResult.Outcome == OutcomeType.Successful)
            {
                result.IsSuccess = true;
                message.SendDate = DateTime.Now;
            }
            else
            {
                result.ErrorMessage = policyResult.FinalException?.Message;
                message.LastSendError = policyResult.FinalException?.ToString();
            }

            await _notificationMessageService.SaveNotificationMessagesAsync(new[] { message });

            return result;
        }

        private async Task<NotificationMessage> CreateMessageAsync(Notification notification)
        {
            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message");
            message.MaxSendAttemptCount = _maxRetryAttempts + 1;
            await notification.ToMessageAsync(message, _notificationTemplateRender);
            await _notificationMessageService.SaveNotificationMessagesAsync(new[] { message });

            return message;
        }
    }
}
