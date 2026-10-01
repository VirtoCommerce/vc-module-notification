using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Web.Controllers;
using VirtoCommerce.NotificationsModule.Web.Model;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationsControllerUnitTests
    {
        private const string BindingErrorKey = "data.createdDate";

        private readonly NotificationsController _controller;

        public NotificationsControllerUnitTests()
        {
            _controller = new NotificationsController(
                Mock.Of<INotificationSearchService>(),
                Mock.Of<INotificationService>(),
                Mock.Of<INotificationTemplateRenderer>(),
                Mock.Of<INotificationSender>(),
                Mock.Of<INotificationMessageSearchService>(),
                Mock.Of<INotificationMessageService>(),
                Mock.Of<INotificationMessageSenderFactory>());

            // What MVC leaves behind when the body fails to deserialize
            _controller.ModelState.AddModelError(BindingErrorKey, "Could not convert string to DateTime");
        }

        public static TheoryData<NotificationTemplateRequest> UnboundRequests => new()
        {
            null,
            new NotificationTemplateRequest { Data = null },
        };

        [Theory]
        [MemberData(nameof(UnboundRequests))]
        public async Task RenderingTemplate_RequestBodyDidNotBind_ReturnsBadRequestWithModelStateErrors(NotificationTemplateRequest request)
        {
            var result = await _controller.RenderingTemplate(request, "default");

            AssertBadRequestWithBindingError(result);
        }

        [Theory]
        [MemberData(nameof(UnboundRequests))]
        public async Task SharePreview_RequestBodyDidNotBind_ReturnsBadRequestWithModelStateErrors(NotificationTemplateRequest request)
        {
            var result = await _controller.SharePreview(request, "default");

            AssertBadRequestWithBindingError(result.Result);
        }

        private static void AssertBadRequestWithBindingError(IActionResult result)
        {
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsType<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey(BindingErrorKey));
        }
    }
}
