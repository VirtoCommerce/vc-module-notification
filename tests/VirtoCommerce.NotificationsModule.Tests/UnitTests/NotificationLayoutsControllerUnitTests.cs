using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Web.Controllers;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationLayoutsControllerUnitTests
    {
        private readonly Mock<INotificationLayoutService> _layoutServiceMock;
        private readonly Mock<INotificationLayoutSearchService> _layoutSearchServiceMock;
        private readonly NotificationLayoutRegistrar _layoutRegistrar;
        private readonly NotificationLayoutsController _controller;

        public NotificationLayoutsControllerUnitTests()
        {
            _layoutServiceMock = new Mock<INotificationLayoutService>();
            _layoutSearchServiceMock = new Mock<INotificationLayoutSearchService>();
            _layoutRegistrar = new NotificationLayoutRegistrar();
            _controller = new NotificationLayoutsController(
                _layoutServiceMock.Object,
                _layoutSearchServiceMock.Object,
                _layoutRegistrar);
        }

        // UpdateNotificationLayout

        [Fact]
        public async Task UpdateNotificationLayout_NonPredefinedLayout_IdUnchanged()
        {
            // Arrange — custom layout not registered in the registrar
            var layout = new NotificationLayout { Id = "some-uuid", Name = "Custom Layout" };
            _layoutSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>()))
                .ReturnsAsync(new NotificationLayoutSearchResult());

            // Act
            await _controller.UpdateNotificationLayout(layout);

            // Assert — Id must remain unchanged
            _layoutServiceMock.Verify(x => x.SaveChangesAsync(
                It.Is<IList<NotificationLayout>>(list => list[0].Id == "some-uuid")), Times.Once);
        }

        [Fact]
        public async Task UpdateNotificationLayout_PredefinedLayout_NoExistingDbRecord_IdSetToNull()
        {
            // Arrange — predefined layout opened for the first time (id == name, no DB record yet)
            const string name = "Default";
            _layoutRegistrar.RegisterLayout(name, "<div>{{ content }}</div>");
            var layout = new NotificationLayout { Id = name, Name = name };

            // Generic fallback first, specific override second (Moq: last matching setup wins)
            _layoutSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult());
            _layoutSearchServiceMock
                .Setup(x => x.SearchAsync(It.Is<NotificationLayoutSearchCriteria>(c => c.Names != null && c.Names.Contains(name)), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult { Results = [] });

            // Act
            await _controller.UpdateNotificationLayout(layout);

            // Assert — Id cleared so EF generates a fresh UUID (INSERT, not UPDATE)
            _layoutServiceMock.Verify(x => x.SaveChangesAsync(
                It.Is<IList<NotificationLayout>>(list => list[0].Id == null)), Times.Once);
        }

        [Fact]
        public async Task UpdateNotificationLayout_PredefinedLayout_ExistingDbRecord_IdSetToExistingUuid()
        {
            // Arrange — predefined layout was previously saved to DB with name as id (legacy row)
            const string name = "Default";
            const string existingUuid = "real-uuid-from-db";
            _layoutRegistrar.RegisterLayout(name, "<div>{{ content }}</div>");
            var layout = new NotificationLayout { Id = name, Name = name };

            // Generic fallback first, specific override second (Moq: last matching setup wins)
            _layoutSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult());
            _layoutSearchServiceMock
                .Setup(x => x.SearchAsync(It.Is<NotificationLayoutSearchCriteria>(c => c.Names != null && c.Names.Contains(name)), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult
                {
                    Results = [new NotificationLayout { Id = existingUuid, Name = name }]
                });

            // Act
            await _controller.UpdateNotificationLayout(layout);

            // Assert — existing UUID reused → UPDATE, not INSERT → no duplicate-key error
            _layoutServiceMock.Verify(x => x.SaveChangesAsync(
                It.Is<IList<NotificationLayout>>(list => list[0].Id == existingUuid)), Times.Once);
        }

        // ResetNotificationLayoutToDefault

        [Fact]
        public async Task ResetNotificationLayoutToDefault_PredefinedOverride_DeletesAndReturnsNoContent()
        {
            // Arrange
            const string id = "real-uuid";
            const string name = "Default";
            _layoutRegistrar.RegisterLayout(name, "<div>{{ content }}</div>");
            _layoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains(id)), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new NotificationLayout { Id = id, Name = name }]);

            // Act
            var result = await _controller.ResetNotificationLayoutToDefault(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _layoutServiceMock.Verify(x => x.DeleteAsync(new[] { id }), Times.Once);
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
            _layoutServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task ResetNotificationLayoutToDefault_CustomLayout_ReturnsNotFound()
        {
            // Arrange — DB record exists but it's a user-created layout (not registered as predefined)
            const string id = "custom-uuid";
            _layoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains(id)), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([new NotificationLayout { Id = id, Name = "My Custom Layout" }]);

            // Act
            var result = await _controller.ResetNotificationLayoutToDefault(id);

            // Assert — custom layouts cannot be reset
            Assert.IsType<NotFoundResult>(result);
            _layoutServiceMock.Verify(x => x.DeleteAsync(It.IsAny<string[]>()), Times.Never);
        }
    }
}
