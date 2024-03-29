using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly;
using VirtoCommerce.NotificationsModule.Core;
using VirtoCommerce.NotificationsModule.Core.Exceptions;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Web.Extensions;
using VirtoCommerce.NotificationsModule.Web.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Web.Controllers
{
    [Authorize(ModuleConstants.Security.Permissions.Access)]
    public class NotificationsController : Controller
    {
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationService _notificationService;
        private readonly INotificationTemplateRenderer _notificationTemplateRender;
        private readonly INotificationSender _notificationSender;
        private readonly INotificationMessageSearchService _notificationMessageSearchService;
        private readonly INotificationMessageService _notificationMessageService;
        private readonly INotificationMessageSenderFactory _notificationMessageSenderFactory;

        public NotificationsController(
            INotificationSearchService notificationSearchService,
            INotificationService notificationService,
            INotificationTemplateRenderer notificationTemplateRender,
            INotificationSender notificationSender,
            INotificationMessageSearchService notificationMessageSearchService,
            INotificationMessageService notificationMessageService,
            INotificationMessageSenderFactory notificationMessageSenderFactory)
        {
            _notificationSearchService = notificationSearchService;
            _notificationService = notificationService;
            _notificationTemplateRender = notificationTemplateRender;
            _notificationSender = notificationSender;
            _notificationMessageSearchService = notificationMessageSearchService;
            _notificationMessageService = notificationMessageService;
            _notificationMessageSenderFactory = notificationMessageSenderFactory;
        }

        /// <summary>
        /// Get all registered notification types by criteria
        /// </summary>
        /// <param name="searchCriteria">criteria for search(keyword, skip, take and etc.)</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/notifications")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<NotificationSearchResult>> GetNotifications([FromBody] NotificationSearchCriteria searchCriteria)
        {
            var notifications = await _notificationSearchService.SearchNotificationsAsync(searchCriteria);

            return Ok(notifications);
        }

        /// <summary>
        /// Get notification by type
        /// </summary>
        /// <param name="type">Notification type of template</param>
        /// <param name="tenantId">Tenant id of template</param>
        /// <param name="tenantType">Tenant type id of template</param>
        /// <remarks>
        /// Get all notification templates by notification type, tenantId, teantTypeId. Tenant id and tenant type id - params of tenant, that initialize creating of
        /// template. By default tenant id and tenant type id = "Platform". For example for store with id = "SampleStore", tenantId = "SampleStore", tenantType = "Store".
        /// </remarks>
        /// <returns></returns>
        [HttpGet]
        [Route("api/notifications/{type}")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<Notification>> GetNotificationByTypeId(string type, string tenantId = null, string tenantType = null)
        {
            var notification = await _notificationSearchService.GetNotificationAsync(type, new TenantIdentity(tenantId, tenantType));

            return Ok(notification);
        }

        /// <summary>
        /// Update notification with templates
        /// </summary>
        /// <param name="notification">Notification</param>
        /// <returns></returns>
        [HttpPut]
        [Route("api/notifications/{type}")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> UpdateNotification([FromBody] Notification notification)
        {
            await _notificationService.SaveChangesAsync(new[] { notification });

            return NoContent();
        }

        /// <summary>
        /// Render content
        /// </summary>
        /// <param name="language"></param>
        /// <param name="request">request of Notification Template with text and data</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/notifications/{type}/templates/{language}/rendercontent")]
        [Authorize(ModuleConstants.Security.Permissions.ReadTemplates)]
        public async Task<ActionResult> RenderingTemplate([FromBody] NotificationTemplateRequest request, string language)
        {
            var template = request.Data.Templates.FindTemplateForLanguage(language);

            var context = new NotificationRenderContext
            {
                Template = request.Text,
                Model = request.Data,
                Language = template.LanguageCode,
                LayoutId = request.NotificationLayoutId,
            };

            var result = await _notificationTemplateRender.RenderAsync(context);

            return Ok(new { html = result });
        }


        /// <summary>
        /// Send notification preview
        /// </summary>
        /// <param name="language"></param>
        /// <param name="request">request of Notification Template with text and data</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/notifications/{type}/templates/{language}/sharepreview")]
        [Authorize(ModuleConstants.Security.Permissions.ReadTemplates)]
        public async Task<ActionResult<NotificationSendResult>> SharePreview([FromBody] NotificationTemplateRequest request, string language)
        {
            var notification = request.Data;
            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message");

            await notification.ToMessageAsync(message, _notificationTemplateRender);

            var policy = Policy.Handle<SentNotificationException>().WaitAndRetryAsync(message.MaxSendAttemptCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(3, retryAttempt)));

            var policyResult = await policy.ExecuteAndCaptureAsync(() =>
            {
                message.LastSendAttemptDate = DateTime.UtcNow;
                message.SendAttemptCount++;
                return _notificationMessageSenderFactory.GetSender(message).SendNotificationAsync(message);
            });

            var result = new NotificationSendResult();

            if (policyResult.Outcome == OutcomeType.Successful)
            {
                result.IsSuccess = true;
                message.SendDate = DateTime.UtcNow;
                message.Status = NotificationMessageStatus.Sent;
            }
            else
            {
                var errorDetails = policyResult.FinalException?.Message.ToString();
                result.ErrorMessage = $"Failed to send message. Error Details: {errorDetails}";
                message.LastSendError = errorDetails;
                message.Status = NotificationMessageStatus.Error;
            }

            return Ok(result);
        }

        /// <summary>
        /// Sending notification
        /// </summary>
        [HttpPost]
        [Route("api/notifications/send")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<NotificationSendResult>> SendNotification([FromBody] Notification notificationRequest)
        {
            var notification = await _notificationSearchService.GetNotificationAsync(notificationRequest.Type, notificationRequest.TenantIdentity);
            var result = await _notificationSender.SendNotificationAsync(notification.PopulateFromOther(notificationRequest));

            return Ok(result);
        }

        /// <summary>
        /// Schedule sending notification
        /// </summary>
        [HttpPost]
        [Route("api/notifications/schedule")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult> ScheduleSendNotification([FromBody] Notification notificationRequest)
        {
            var notification = await _notificationSearchService.GetNotificationAsync(notificationRequest.Type, notificationRequest.TenantIdentity);
            await _notificationSender.ScheduleSendNotificationAsync(notification.PopulateFromOther(notificationRequest));

            return Ok();
        }

        /// <summary>
        /// Schedule resending notification
        /// </summary>
        [HttpPost]
        [Route("api/notifications/scheduleresend")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<IActionResult> ResendNotifications([FromBody] string[] messageIds)
        {
            var messages = await _notificationMessageService.GetNotificationsMessageByIds(messageIds);

            if (!messages.Any())
            {
                return NotFound("Messages to send not found");
            }

            foreach (var message in messages)
            {
                message.Id = null;
                message.CreatedDate = DateTime.UtcNow;
                message.Status = NotificationMessageStatus.Pending;
                message.SendAttemptCount = 0;
                message.LastSendAttemptDate = null;
                message.LastSendError = null;
                message.SendDate = null;
            }

            await _notificationMessageService.SaveNotificationMessagesAsync(messages);

            foreach (var id in messages.Select(x => x.Id))
            {
                _notificationSender.EnqueueNotificationSending(id);
            }

            return Ok();
        }

        /// <summary>
        /// Sending notification
        /// </summary>
        /// <remarks>
        /// Method sending notification, that based on notification template. Template for rendering chosen by type, objectId, objectTypeId, language.
        /// Parameters for template may be prepared by the method of getTestingParameters. Method returns string. If sending finished with success status
        /// this string is empty, otherwise string contains error message.
        /// </remarks>
        /// <param name="request">Notification request</param>
        [HttpPost]
        [Route("api/platform/notification/template/sendnotification")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<NotificationSendResult>> SendNotificationByRequest([FromBody] NotificationRequest request)
        {
            var notification = await _notificationSearchService.GetNotificationAsync(request.Type, new TenantIdentity(request.ObjectId, request.ObjectTypeId));

            if (notification == null)
            {
                return new NotificationSendResult { ErrorMessage = $"{request.Type} isn't registered", IsSuccess = false };
            }

            PopulateNotification(request, notification);
            var result = await _notificationSender.SendNotificationAsync(notification);

            return Ok(result);
        }

        /// <summary>
        /// Get all notification journal
        /// </summary>
        /// <remarks>
        /// Method returns notification journal page with array of notification, that was send, sending or will be send in future. Result contains total count, that can be used
        /// for paging.
        /// </remarks>
        /// <param name="criteria"></param>
        [HttpPost]
        [Route("api/notifications/journal")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<NotificationMessageSearchResult>> GetNotificationJournal([FromBody] NotificationMessageSearchCriteria criteria)
        {
            var result = await _notificationMessageSearchService.SearchMessageAsync(criteria);

            return Ok(result);
        }

        [HttpGet]
        [Route("api/notifications/journal/{id}")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<NotificationMessage>> GetObjectNotificationJournal(string id)
        {
            var result = (await _notificationMessageService.GetNotificationsMessageByIds(new[] { id })).FirstOrDefault();

            return Ok(result);
        }

        private void PopulateNotification(NotificationRequest request, Notification notification)
        {
            notification.TenantIdentity = new TenantIdentity(request.ObjectId, request.ObjectTypeId);
            notification.LanguageCode = request.Language;

            foreach (var parameter in request.NotificationParameters)
            {
                notification.SetValue(parameter);
            }
        }
    }
}
