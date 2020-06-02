using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.Platform.Caching;
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
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;
        private readonly IPlatformMemoryCache _memCache;
        private readonly Mock<ICacheEntry> _cacheEntryMock;

        public NotificationServiceUnitTests()
        {
            _repositoryMock = new Mock<INotificationRepository>();
            _repositoryFactory = () => _repositoryMock.Object;
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _repositoryMock.Setup(ss => ss.UnitOfWork).Returns(_mockUnitOfWork.Object);
            _eventPublisherMock = new Mock<IEventPublisher>();
            _memCache = GetCache();
            _cacheEntryMock = new Mock<ICacheEntry>();
            _cacheEntryMock.SetupGet(c => c.ExpirationTokens).Returns(new List<IChangeToken>());

            _notificationSearchServiceMock = new Mock<INotificationSearchService>();

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
            var service = GetNotificationService();
            var cacheKey = CacheKey.With(service.GetType(), nameof(service.GetByIdsAsync), string.Join("-", new[] { id }), responseGroup);

            //Act
            var result = await service.GetByIdsAsync(new[] { id }, responseGroup);

            //Assert
            Assert.NotNull(result);
            Assert.Contains(result, r => r.Id.Equals(id));
            Assert.Contains(type, result.Select(x => x.Type));
        }

        [Fact(Skip = "fail. Temporary disabled. TODO")]
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
            var service = GetNotificationService();

            //Act
            await service.SaveChangesAsync(notifications.ToArray());
        }

        private static IPlatformMemoryCache GetCache()
        {
            var defaultOptions = Options.Create(new CachingOptions() { CacheSlidingExpiration = TimeSpan.FromMilliseconds(10) });
            var logger = new Moq.Mock<ILogger<PlatformMemoryCache>>();
            return new PlatformMemoryCache(new MemoryCache(new MemoryCacheOptions()), defaultOptions, logger.Object);
        }

        [Fact]
        public async Task GetByIdsAsync_GetThenSaveNotification_ReturnCachedNotification()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var newNotification = new ConfirmationEmailNotification { Id = id };
            var newNotificationEntity = AbstractTypeFactory<NotificationEntity>.TryCreateInstance(nameof(EmailNotificationEntity)).FromModel(newNotification, new PrimaryKeyResolvingMap());
            var service = GetNotificationServiceWithPlatformMemoryCache();
            _repositoryMock.Setup(x => x.Add(newNotificationEntity.ResetEntityData()))
                .Callback(() =>
                {
                    _repositoryMock.Setup(o => o.GetByIdsAsync(new[] { id }, null))
                        .ReturnsAsync(new[] { newNotificationEntity });
                });

            //Act
            var emptyNotifications = await service.GetByIdsAsync(new[] { id }, null);
            await service.SaveChangesAsync(new[] { newNotification });
            var notifications = await service.GetByIdsAsync(new[] { id }, null);

            //Assert
            Assert.NotEqual(emptyNotifications, notifications);
        }

        private NotificationService GetNotificationService()
        {
            return GetNotificationService(_memCache);
        }

        private NotificationService GetNotificationServiceWithPlatformMemoryCache()
        {
            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var platformMemoryCache = new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);

            return GetNotificationService(platformMemoryCache);
        }

        private NotificationService GetNotificationService(IPlatformMemoryCache platformMemoryCache)
        {
            return new NotificationService(() => _repositoryMock.Object, _eventPublisherMock.Object, platformMemoryCache);
        }
    }
}
