using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Web.Controllers;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationLayoutsControllerUnitTests
    {
        private readonly Mock<INotificationLayoutService> _layoutServiceMock;
        private readonly Mock<INotificationLayoutSearchService> _layoutSearchServiceMock;
        private readonly Mock<INotificationLayoutUsageService> _layoutUsageServiceMock;
        private readonly NotificationLayoutsController _controller;

        public NotificationLayoutsControllerUnitTests()
        {
            _layoutServiceMock = new Mock<INotificationLayoutService>();
            _layoutSearchServiceMock = new Mock<INotificationLayoutSearchService>();
            _layoutUsageServiceMock = new Mock<INotificationLayoutUsageService>();
            _controller = new NotificationLayoutsController(
                _layoutServiceMock.Object,
                _layoutSearchServiceMock.Object,
                _layoutUsageServiceMock.Object);
        }

        // UpdateNotificationLayout

        [Fact]
        public async Task UpdateNotificationLayout_NonPredefinedLayout_IdUnchanged()
        {
            // Arrange — custom layout not registered in the registrar
            var layout = new NotificationLayout { Id = "some-uuid", Name = "Custom Layout" };
            _layoutSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult());

            // Act
            await _controller.UpdateNotificationLayout(layout);

            // Assert — Id must remain unchanged
            _layoutServiceMock.Verify(x => x.SaveChangesAsync(
                It.Is<IList<NotificationLayout>>(list => list[0].Id == "some-uuid")), Times.Once);
        }

        // ResetNotificationLayoutToDefault

        [Fact]
        public async Task ResetNotificationLayoutToDefault_PredefinedOverride_DeletesAndReturnsNoContent()
        {
            // Arrange — DB override of a predefined layout
            const string id = "real-uuid";
            const string name = "Default";
            _layoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains(id)), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new NotificationLayout { Id = id, Name = name, IsPredefined = true }]);

            // Act
            var result = await _controller.ResetNotificationLayoutToDefault(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _layoutServiceMock.Verify(x => x.DeleteAsync(new[] { id }, It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task ResetNotificationLayoutToDefault_PredefinedNoDbOverride_ReturnsNoContentWithoutDelete()
        {
            // Arrange — predefined layout with no DB override (id == name)
            const string name = "Default";
            _layoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains(name)), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new NotificationLayout { Id = name, Name = name, IsPredefined = true }]);

            // Act
            var result = await _controller.ResetNotificationLayoutToDefault(name);

            // Assert — nothing to delete, but still NoContent
            Assert.IsType<NoContentResult>(result);
            _layoutServiceMock.Verify(x => x.DeleteAsync(It.IsAny<IList<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task ResetNotificationLayoutToDefault_NoDbRecord_ReturnsNotFound()
        {
            // Arrange
            _layoutServiceMock
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([]);

            // Act
            var result = await _controller.ResetNotificationLayoutToDefault("nonexistent-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _layoutServiceMock.Verify(x => x.DeleteAsync(It.IsAny<IList<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task ResetNotificationLayoutToDefault_CustomLayout_ReturnsNotFound()
        {
            // Arrange — DB record exists but it's a user-created layout (not predefined)
            const string id = "custom-uuid";
            _layoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains(id)), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new NotificationLayout { Id = id, Name = "My Custom Layout", IsPredefined = false }]);

            // Act
            var result = await _controller.ResetNotificationLayoutToDefault(id);

            // Assert — custom layouts cannot be reset
            Assert.IsType<NotFoundResult>(result);
            _layoutServiceMock.Verify(x => x.DeleteAsync(It.IsAny<IList<string>>(), It.IsAny<bool>()), Times.Never);
        }
        [Fact]
        public async Task ResetNotificationLayoutToDefault_CalledWithNameOfOverriddenLayout_DeletesOverrideById()
        {
            // Arrange — the service resolves the predefined name to its DB override
            _layoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains("Default")), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new NotificationLayout { Id = "real-uuid", Name = "Default", IsPredefined = true }]);

            // Act
            var result = await _controller.ResetNotificationLayoutToDefault("Default");

            // Assert
            Assert.IsType<NoContentResult>(result);
            _layoutServiceMock.Verify(x => x.DeleteAsync(new[] { "real-uuid" }, It.IsAny<bool>()), Times.Once);
        }

        // GetNotificationLayoutUsage

        [Fact]
        public async Task GetNotificationLayoutUsage_LayoutExists_ReturnsUsage()
        {
            // Arrange
            var usage = new NotificationLayoutUsage { LayoutId = "Default", NotificationTypes = ["OrderCreateEmailNotification"] };
            _layoutUsageServiceMock.Setup(x => x.GetUsageAsync("Default")).ReturnsAsync(usage);

            // Act
            var result = await _controller.GetNotificationLayoutUsage("Default");

            // Assert
            Assert.Same(usage, Assert.IsType<OkObjectResult>(result.Result).Value);
        }

        [Fact]
        public async Task GetNotificationLayoutUsage_LayoutMissing_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetNotificationLayoutUsage("missing");

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
