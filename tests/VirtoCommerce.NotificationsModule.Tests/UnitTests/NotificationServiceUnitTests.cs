using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using Xunit;
using Assert = Xunit.Assert;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationServiceUnitTests
    {
        private readonly Mock<INotificationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly INotificationRegistrar _notificationRegistrar;
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;
        private readonly Mock<IPlatformMemoryCache> _platformMemoryCacheMock;
        private readonly Mock<ICacheEntry> _cacheEntryMock;
        private readonly NotificationService _notificationService;

        public NotificationServiceUnitTests()
        {
            _repositoryMock = new Mock<INotificationRepository>();
            _repositoryFactory = () => _repositoryMock.Object;
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _repositoryMock.Setup(ss => ss.UnitOfWork).Returns(_mockUnitOfWork.Object);
            _eventPublisherMock = new Mock<IEventPublisher>();
            _platformMemoryCacheMock = new Mock<IPlatformMemoryCache>();
            _cacheEntryMock = new Mock<ICacheEntry>();
            _cacheEntryMock.SetupGet(c => c.ExpirationTokens).Returns(new List<IChangeToken>());

            _notificationService = new NotificationService(_repositoryFactory, _eventPublisherMock.Object, _platformMemoryCacheMock.Object);
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _notificationRegistrar = new NotificationRegistrar(_notificationService, _notificationSearchServiceMock.Object);

            if (!AbstractTypeFactory<NotificationEntity>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationEntity)))
                AbstractTypeFactory<NotificationEntity>.RegisterType<EmailNotificationEntity>();
        }

        [Fact]
        public async Task GetNotificationsByIdsAsync_ReturnNotifications()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var type = nameof(RegistrationEmailNotification);
            var responseGroup = NotificationResponseGroup.Full.ToString();
            var notifications = new List<NotificationEntity> { new EmailNotificationEntity() { Id = id, Type = type } };
            _repositoryMock.Setup(n => n.GetByIdsAsync(new[] { id }, responseGroup))
                .ReturnsAsync(notifications.ToArray());
            var criteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            criteria.Take = 1;

            criteria.NotificationType = type;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria)).ReturnsAsync(new NotificationSearchResult());
            //TODO
            //_notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();

            var cacheKey = CacheKey.With(_notificationService.GetType(), nameof(_notificationService.GetByIdsAsync), string.Join("-", new[] { id }), responseGroup);
            _platformMemoryCacheMock.Setup(pmc => pmc.CreateEntry(cacheKey)).Returns(_cacheEntryMock.Object);

            //Act
            var result = await _notificationService.GetByIdsAsync(new[] { id }, responseGroup);

            //Assert
            Assert.NotNull(result);
            Assert.Contains(result, r => r.Id.Equals(id));
            Assert.Contains(type, result.Select(x => x.Type));
        }

        [Fact]
        public async Task SaveChangesAsync_SavedNotification()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var notificationEntities = new List<NotificationEntity>
            {
                new EmailNotificationEntity()
                {
                    Id = id,
                    Type = nameof(EmailNotification),
                    Kind = nameof(EmailNotification)
                }
            };
            var responseGroup = NotificationResponseGroup.Default.ToString();
            _repositoryMock.Setup(n => n.GetByIdsAsync(new[] { id }, responseGroup))
                .ReturnsAsync(notificationEntities.ToArray());
            var notifications = notificationEntities.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type)));

            //Act
            await _notificationService.SaveChangesAsync(notifications.ToArray());
        }
    }
}
