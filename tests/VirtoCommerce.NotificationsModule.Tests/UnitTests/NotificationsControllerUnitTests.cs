using System;
using System.Text.Json;
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
        private const string BindingErrorKey = "data.cc";

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
        }

        public static TheoryData<NotificationTemplateRequest> UnboundRequests => new()
        {
            null,
            new NotificationTemplateRequest { Data = null },
        };

        [Theory]
        [MemberData(nameof(UnboundRequests))]
        public async Task RenderingTemplate_RequestBodyDidNotBind_ReturnsBadRequestNamingTheField(NotificationTemplateRequest request)
        {
            AddBindingError();

            var result = await _controller.RenderingTemplate(request, "default");

            AssertBadRequestMessageContains(result, BindingErrorKey);
        }

        [Theory]
        [MemberData(nameof(UnboundRequests))]
        public async Task SharePreview_RequestBodyDidNotBind_ReturnsBadRequestNamingTheField(NotificationTemplateRequest request)
        {
            AddBindingError();

            var result = await _controller.SharePreview(request, "default");

            AssertBadRequestMessageContains(result.Result, BindingErrorKey);
        }

        [Fact]
        public async Task RenderingTemplate_RequestBodyMissingWithoutModelStateErrors_ReturnsBadRequestWithGenericMessage()
        {
            var result = await _controller.RenderingTemplate(null, "default");

            AssertBadRequestMessageContains(result, "missing or invalid");
        }

        // What MVC leaves in ModelState when the body fails to deserialize: an exception, no message text
        private void AddBindingError()
        {
            _controller.ModelState.TryAddModelException(BindingErrorKey, new FormatException("Could not convert value to String"));
        }

        // The editor reads only "message" from the error body
        private static void AssertBadRequestMessageContains(IActionResult result, string expected)
        {
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            using var body = JsonDocument.Parse(JsonSerializer.Serialize(badRequest.Value));

            Assert.Contains(expected, body.RootElement.GetProperty("message").GetString());
        }
    }
}
