using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationBuilderUnitTests
    {
        [Theory]
        [InlineData(true, "Default")]
        [InlineData(false, "4f1c2d")]
        public void WithLayout_LayoutFound_PredefinedReferencedByName(bool isPredefined, string expectedLayoutId)
        {
            // The search returns the DB override of the predefined layout, which has its own id
            var searchService = new Mock<INotificationLayoutSearchService>();
            searchService
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult
                {
                    Results = new List<NotificationLayout> { new() { Id = "4f1c2d", Name = "Default", IsPredefined = isPredefined } },
                });
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(INotificationLayoutSearchService))).Returns(searchService.Object);

            var notification = new NotificationBuilder(serviceProvider.Object, typeof(SampleEmailNotification))
                .WithTemplates(new EmailNotificationTemplate { Subject = "s", Body = "b" })
                .WithLayout("Default")
                .Build();

            Assert.Equal(expectedLayoutId, notification.Templates.OfType<EmailNotificationTemplate>().Single().NotificationLayoutId);
        }
    }
}
