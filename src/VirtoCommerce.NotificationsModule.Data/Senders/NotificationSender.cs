using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Polly;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Exceptions;

namespace VirtoCommerce.NotificationsModule.Data.Senders
{
    public class NotificationSender : INotificationSender
    {
        private readonly int _maxRetryAttempts = 1;
        private readonly INotificationTemplateRenderer _notificationTemplateRender;
        private readonly INotificationMessageService _notificationMessageService;
        private readonly INotificationMessageSenderFactory _notificationMessageSenderFactory;
        private readonly IBackgroundJobClient _jobClient;

        public NotificationSender(INotificationTemplateRenderer notificationTemplateRender
            , INotificationMessageService notificationMessageService
            , INotificationMessageSenderFactory notificationMessageAccessor
            , IBackgroundJobClient jobClient)
        {
            _notificationTemplateRender = notificationTemplateRender;
            _notificationMessageService = notificationMessageService;
            _notificationMessageSenderFactory = notificationMessageAccessor;
            _jobClient = jobClient;
        }

        public async Task ScheduleSendNotificationAsync(Notification notification)
        {
            if (notification.IsActive.Value)
            {
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

            if (notification.IsActive.GetValueOrDefault())
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
                message.LastSendAttemptDate = DateTime.Now;
                message.SendAttemptCount++;
                return _notificationMessageSenderFactory.GetSender(message).SendNotificationAsync(message);
            });

            if (policyResult.Outcome == OutcomeType.Successful)
            {
                result.IsSuccess = true;
                message.SendDate = DateTime.Now;
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
