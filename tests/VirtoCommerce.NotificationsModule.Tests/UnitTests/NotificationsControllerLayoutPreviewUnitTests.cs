using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.NotificationsModule.Web.Controllers;
using VirtoCommerce.NotificationsModule.Web.Model;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    /// <summary>
    /// rendercontent with an unsaved layout text and a store, used by the layout preview blade.
    /// </summary>
    public class NotificationsControllerLayoutPreviewUnitTests
    {
        private readonly Mock<INotificationTemplateRenderer> _renderer = new();
        private readonly NotificationsController _controller;
        private NotificationRenderContext _context;

        public NotificationsControllerLayoutPreviewUnitTests()
        {
            _renderer
                .Setup(x => x.RenderAsync(It.IsAny<NotificationRenderContext>()))
                .Callback<NotificationRenderContext>(x => _context = x)
                .ReturnsAsync("html");

            _controller = new NotificationsController(
                Mock.Of<INotificationSearchService>(),
                Mock.Of<INotificationService>(),
                _renderer.Object,
                Mock.Of<INotificationSender>(),
                Mock.Of<INotificationMessageSearchService>(),
                Mock.Of<INotificationMessageService>(),
                Mock.Of<INotificationMessageSenderFactory>());
        }

        [Fact]
        public async Task RenderingTemplate_LayoutTemplate_AppendedToTextInsteadOfLayoutId()
        {
            var request = Request();
            request.NotificationLayoutId = "Default";
            request.LayoutTemplate = "<main>{{ content }}</main>";

            var result = await _controller.RenderingTemplate(request, "en-US");

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("{% capture content %}Hi{% endcapture %}<main>{{ content }}</main>", _context.Template);
            Assert.Null(_context.LayoutId);
        }

        [Fact]
        public async Task RenderingTemplate_NoLayoutTemplate_LayoutIdKept()
        {
            var request = Request();
            request.NotificationLayoutId = "Default";

            await _controller.RenderingTemplate(request, "en-US");

            Assert.Equal("{% capture content %}Hi{% endcapture %}", _context.Template);
            Assert.Equal("Default", _context.LayoutId);
        }

        [Fact]
        public async Task RenderingTemplate_StoreId_NotificationRenderedForStore()
        {
            var request = Request();
            request.StoreId = "store-1";

            await _controller.RenderingTemplate(request, "en-US");

            var notification = Assert.IsType<SampleEmailNotification>(_context.Model);
            Assert.Equal(new TenantIdentity("store-1", "Store"), notification.TenantIdentity);
        }

        private static NotificationTemplateRequest Request()
        {
            return new NotificationTemplateRequest
            {
                Text = "{% capture content %}Hi{% endcapture %}",
                Data = new SampleEmailNotification
                {
                    Templates = new List<NotificationTemplate> { new EmailNotificationTemplate { LanguageCode = "en-US" } },
                },
            };
        }
    }
}
