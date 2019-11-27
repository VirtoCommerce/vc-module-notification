using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationSearchServiceUnitTests
    {
        private readonly Mock<INotificationRepository> _repositoryMock;
        private readonly Mock<Func<INotificationRepository>> _repositoryFactoryMock;
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly INotificationRegistrar _notificationRegistrar;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly NotificationSearchService _notificationSearchService;

        public NotificationSearchServiceUnitTests()
        {
            _repositoryMock = new Mock<INotificationRepository>();
            _repositoryFactoryMock = new Mock<Func<INotificationRepository>>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _notificationRegistrar = new NotificationRegistrar();
            _repositoryFactory = () => _repositoryMock.Object;
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchService = new NotificationSearchService(_repositoryFactory, _notificationServiceMock.Object);

            _notificationRegistrar.RegisterNotification<InvoiceEmailNotification>();
            _notificationRegistrar.RegisterNotification<OrderSentEmailNotification>();
            _notificationRegistrar.RegisterNotification<OrderPaidEmailNotification>();
            _notificationRegistrar.RegisterNotification<RemindUserNameEmailNotification>();
            _notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();
        }

        [Fact]
        public async Task GetNotificationByTypeAsync_ReturnNotification()
        {
            //Arrange
            var type = nameof(RegistrationEmailNotification);

            var mockNotifications = new Common.TestAsyncEnumerable<NotificationEntity>(new List<NotificationEntity>());
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.AsQueryable());
            _notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();

            //Act
            var result = await _notificationSearchService.GetNotificationAsync(type);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(type, result.Type);
        }

        [Fact]
        public async Task GetNotificationByAliasAsync_ReturnTransientNotification()
        {
            //Arrange
            var type = "RemindUserNameNotification";

            var mockNotifications = new Common.TestAsyncEnumerable<NotificationEntity>(new List<NotificationEntity>());
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.AsQueryable());


            //Act
            var result = await _notificationSearchService.GetNotificationAsync(type);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(RemindUserNameEmailNotification), result.Type);
        }

        [Fact]
        public async Task GetNotificationByAliasAsync_ReturnActiveNotification()
        {
            //Arrange
            var type = "RemindUserNameNotification";

            var notifications = new List<NotificationEntity>
            {
                new EmailNotificationEntity
                {
                    Type = nameof(RemindUserNameEmailNotification), Kind = nameof(EmailNotification),
                    Id = Guid.NewGuid().ToString(), IsActive = true
                }
            };

            var mockNotifications = new Common.TestAsyncEnumerable<NotificationEntity>(notifications);
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.AsQueryable());
            var ids = notifications.Select(n => n.Id).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(ids, null))
                .ReturnsAsync(notifications.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray());

            //Act
            var result = await _notificationSearchService.GetNotificationAsync(type);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(nameof(RemindUserNameEmailNotification), result.Type);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task SearchNotificationsAsync_GetOnActiveItemsAndTreeItems()
        {
            //Arrange
            var searchCriteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            searchCriteria.Take = 4;

            var notifications = new List<NotificationEntity>
            {
                new EmailNotificationEntity
                {
                    Type = nameof(RegistrationEmailNotification), Kind = nameof(EmailNotification),
                    Id = Guid.NewGuid().ToString(), IsActive = true
                }
            };

            var mockNotifications = new Common.TestAsyncEnumerable<NotificationEntity>(notifications);
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.AsQueryable());
            var ids = notifications.Select(n => n.Id).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(ids, null))
                .ReturnsAsync(notifications.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray());

            //Act
            var result = await _notificationSearchService.SearchNotificationsAsync(searchCriteria);

            //Assert
            Assert.NotEmpty(result.Results);
            Assert.Equal(4, result.Results.Count());
            Assert.Equal(1, result.Results.Count(r => r.IsActive));
            Assert.Equal(3, result.Results.Count(r => !r.IsActive));
        }

        [Fact]
        public async Task SearchNotificationsAsync_GetTwoItems()
        {
            //Arrange
            var searchCriteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            searchCriteria.Take = 2;
            searchCriteria.Skip = 0;
            var mockNotifications = new Common.TestAsyncEnumerable<NotificationEntity>(new List<NotificationEntity>());
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.AsQueryable());

            //Act
            var result = await _notificationSearchService.SearchNotificationsAsync(searchCriteria);

            //Assert
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SearchNotificationsAsync_ContainsActiveNotifications()
        {
            //Arrange

            var responseGroup = NotificationResponseGroup.Default.ToString();
            var searchCriteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            searchCriteria.Take = 20;
            searchCriteria.ResponseGroup = responseGroup;
            var notificationEntities = new List<NotificationEntity> {
                new EmailNotificationEntity { Type  = nameof(InvoiceEmailNotification), Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), IsActive = true },
                new EmailNotificationEntity { Type  = nameof(OrderSentEmailNotification), Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), IsActive = true },
                new EmailNotificationEntity { Type  = nameof(RegistrationEmailNotification), Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), IsActive = true }
            };
            var mockNotifications = new Common.TestAsyncEnumerable<NotificationEntity>(notificationEntities);
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.AsQueryable());
            var notifications = notificationEntities.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray();
            var ids = notificationEntities.Select(n => n.Id).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(ids, responseGroup))
                .ReturnsAsync(notifications);

            //Act
            var result = await _notificationSearchService.SearchNotificationsAsync(searchCriteria);

            //Assert
            Assert.True(result.Results.Where(n => ids.Contains(n.Id)).All(r => r.IsActive));
        }

        [Theory]
        [ClassData(typeof(PagingTestData))]
        public async Task SearchNotificationsAsync_PagingNotifications(int skip, int take, int activeExpectedCount, int transientExpectedCount, string[] expectedTypes)
        {
            //Arrange
            var responseGroup = NotificationResponseGroup.Default.ToString();
            var searchCriteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            searchCriteria.Take = take;
            searchCriteria.Skip = skip;
            searchCriteria.ResponseGroup = responseGroup;
            var notificationEntities = new List<NotificationEntity> {
                new EmailNotificationEntity { Type  = nameof(InvoiceEmailNotification), Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), IsActive = true },
                new EmailNotificationEntity { Type  = nameof(OrderSentEmailNotification), Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), IsActive = true },
                new EmailNotificationEntity { Type  = nameof(RegistrationEmailNotification), Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), IsActive = true }
            };
            var mockNotifications = new Common.TestAsyncEnumerable<NotificationEntity>(notificationEntities);
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.AsQueryable());
            var notifications = notificationEntities.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(It.IsAny<string[]>(), responseGroup))
                .ReturnsAsync(notifications.Where(n => expectedTypes.Contains(n.Type)).ToArray());

            //Act
            var result = await _notificationSearchService.SearchNotificationsAsync(searchCriteria);

            //Assert
            Assert.Equal(expectedTypes, result.Results.Where(x => x.IsActive).Select(y => y.Type));
            Assert.Equal(activeExpectedCount, result.Results.Count(x => x.IsActive));
            Assert.Equal(transientExpectedCount, result.Results.Count(x => !x.IsActive));
        }

        public class PagingTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { 0, 20, 3, 2, new[] { nameof(InvoiceEmailNotification), nameof(OrderSentEmailNotification), nameof(RegistrationEmailNotification) } };
                yield return new object[] { 0, 3, 2, 1, new[] { nameof(InvoiceEmailNotification), nameof(OrderSentEmailNotification) } };
                yield return new object[] { 1, 3, 1, 2, new[] { nameof(OrderSentEmailNotification) } };
                yield return new object[] { 2, 3, 1, 2, new[] { nameof(RegistrationEmailNotification) } };
                yield return new object[] { 3, 1, 0, 1, Array.Empty<string>() };
                yield return new object[] { 3, 2, 1, 1, new[] { nameof(RegistrationEmailNotification) } };
                yield return new object[] { 4, 1, 0, 1, Array.Empty<string>() };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
