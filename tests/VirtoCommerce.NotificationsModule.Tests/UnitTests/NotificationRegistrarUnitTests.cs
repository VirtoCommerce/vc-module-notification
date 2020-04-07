using System;
using System.Collections.Generic;
using System.Text;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationRegistrarUnitTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;


        private readonly NotificationRegistrar _notificationRegistrar;

        public NotificationRegistrarUnitTests()
        {
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _notificationRegistrar = new NotificationRegistrar(_notificationServiceMock.Object, _notificationSearchServiceMock.Object);
        }

        [Fact]
        public void RegisterNotification_GetNotification()
        {
            //Arrange
            var type = nameof(SampleNotification);
            var criteria4 = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            criteria4.Take = 1;
            criteria4.NotificationType = type;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria4)).ReturnsAsync(new NotificationSearchResult());

            //Act
            _notificationRegistrar.RegisterNotification<SampleNotification>();
            var notification = CreateNotification(type, new UnregisteredNotification());


            //Assert
            Assert.Equal(type, notification.Type);
        }

        [Fact]
        public void OverrideNotificationType_GetNotification()
        {
            //Arrange
            var type = nameof(SampleNotification);
            var criteria4 = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            criteria4.Take = 1;
            criteria4.NotificationType = type;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria4)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.RegisterNotification<SampleNotification>();
            criteria4.NotificationType = nameof(ExtendedSampleNotification);
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria4)).ReturnsAsync(new NotificationSearchResult());

            //Act

            _notificationRegistrar.OverrideNotificationType<SampleNotification, ExtendedSampleNotification>();
            var notification = CreateNotification(type, new UnregisteredNotification());

            //Assert
            Assert.Equal(type, notification.Type);
        }


        private static Notification CreateNotification(string typeName, Notification defaultObj)
        {
            var result = defaultObj;
            var typeInfo = AbstractTypeFactory<Notification>.FindTypeInfoByName(typeName);
            if (typeInfo != null)
            {
                result = AbstractTypeFactory<Notification>.TryCreateInstance(typeName);
            }
            return result;
        }
    }
}
