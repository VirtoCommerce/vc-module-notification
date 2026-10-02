using System;
using System.Linq;
using System.Threading.Tasks;
using Polly;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.BackgroundJobs;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Core.Jobs;

namespace VirtoCommerce.NotificationsModule.Data.Senders
{
    public class NotificationSender : INotificationSender
    {
        private readonly int _maxRetryAttempts = 1;
        private readonly INotificationTemplateRenderer _notificationTemplateRender;
        private readonly INotificationMessageService _notificationMessageService;
        private readonly INotificationMessageSenderFactory _notificationMessageSenderFactory;

        public NotificationSender(INotificationTemplateRenderer notificationTemplateRender
            , INotificationMessageService notificationMessageService
            , INotificationMessageSenderFactory notificationMessageAccessor)
        {
            _notificationTemplateRender = notificationTemplateRender;
            _notificationMessageService = notificationMessageService;
            _notificationMessageSenderFactory = notificationMessageAccessor;
        }

        public async Task ScheduleSendNotificationAsync(Notification notification)
        {
            if (notification.IsActive == true)
            {
                var message = await CreateMessageAsync(notification);

                EnqueueNotificationSending(message.Id);
            }
        }

        public async Task<NotificationSendResult> SendNotificationAsync(Notification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (notification.IsActive.GetValueOrDefault())
            {
                var message = await CreateMessageAsync(notification);

                return await TrySendNotificationMessageAsync(message.Id);
            }

            return new NotificationSendResult();
        }

        public void EnqueueNotificationSending(string messageId)
        {
            _ = BackgroundJob.Enqueue<SendNotificationMessageJob>(new SendNotificationMessagePayload { MessageId = messageId });
        }

        public async Task<NotificationSendResult> TrySendNotificationMessageAsync(string messageId)
        {
            var result = new NotificationSendResult();

            var message = (await _notificationMessageService.GetNotificationsMessageByIds(new[] { messageId })).FirstOrDefault();

            if (message == null)
            {
                result.ErrorMessage = $"Can't find notification message by {messageId}";
                return result;
            }

            if (message.Status == NotificationMessageStatus.Error)
            {
                result.ErrorMessage = $"Can't send notification message by {messageId}. There are errors.";
                return result;
            }

            var policy = Policy.Handle<SentNotificationException>().WaitAndRetryAsync(_maxRetryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)));

            var policyResult = await policy.ExecuteAndCaptureAsync(() =>
            {
                message.LastSendAttemptDate = DateTime.UtcNow;
                message.SendAttemptCount++;
                return _notificationMessageSenderFactory.GetSender(message).SendNotificationAsync(message);
            });

            if (policyResult.Outcome == OutcomeType.Successful)
            {
                result.IsSuccess = true;
                message.SendDate = DateTime.UtcNow;
                message.Status = NotificationMessageStatus.Sent;
            }
            else
            {
                result.ErrorMessage = "Failed to send message.";
                message.LastSendError = policyResult.FinalException?.ToString();
                message.Status = NotificationMessageStatus.Error;
            }

            await _notificationMessageService.SaveNotificationMessagesAsync(new[] { message });

            return result;
        }

        private async Task<NotificationMessage> CreateMessageAsync(Notification notification)
        {
            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message");
            message.MaxSendAttemptCount = _maxRetryAttempts + 1;
            try
            {
                await notification.ToMessageAsync(message, _notificationTemplateRender);
            }
            catch (Exception ex)
            {
                message.LastSendError = ex.ExpandExceptionMessage();
                message.Status = NotificationMessageStatus.Error;
            }
            await _notificationMessageService.SaveNotificationMessagesAsync(new[] { message });

            return message;
        }
    }
}
