using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using Xunit;

namespace VirtoCommerce.NotificationsSampleModule.Tests
{
    public class SocialNetworkNotificationEntity : NotificationEntity
    {
        [StringLength(128)]
        public string Token { get; set; }

        public override Notification ToModel(Notification notification)
        {
            var socialNetworkNotification = notification as SocialNetworkNotification;

            if (socialNetworkNotification != null)
            {
                socialNetworkNotification.Token = Token;
            }

            return base.ToModel(notification);
        }

        public override NotificationEntity FromModel(Notification notification, PrimaryKeyResolvingMap pkMap)
        {
            var socialNetworkNotification = notification as SocialNetworkNotification;
            if (socialNetworkNotification != null)
            {
                Token = socialNetworkNotification.Token;

            }

            return base.FromModel(notification, pkMap);
        }

        public override void Patch(NotificationEntity notification)
        {
            var socialNetworkNotification = notification as SocialNetworkNotificationEntity;
            if (socialNetworkNotification != null)
            {
                socialNetworkNotification.Token = Token;
            }

            base.Patch(notification);
        }
    }

    public abstract class SocialNetworkNotification : Notification
    {
        protected SocialNetworkNotification() : base(nameof(SocialNetworkNotification))
        {
        }

        public string Token { get; set; }
        public override string Kind => nameof(SocialNetworkNotification);
        public override void SetFromToMembers(string from, string to)
        {

        }
    }

    public class SocialNetworkTemplate : NotificationTemplate
    {
        public override string Kind => nameof(SocialNetworkNotification);
    }
    public class SocialNetworkMessage : NotificationMessage
    {
        public override string Kind => nameof(SocialNetworkNotification);
    }
    public class RegistrationSocialNetworkNotification : SocialNetworkNotification { }

    public class SocialNetworkNotificationEntityUnitTests
    {
        private readonly Mock<INotificationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly INotificationRegistrar _notificationRegistrar;
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly NotificationService _notificationService;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly IPlatformMemoryCache _memCache;
        private readonly NotificationSearchService _notificationSearchService;

        public SocialNetworkNotificationEntityUnitTests()
        {
            _repositoryMock = new Mock<INotificationRepository>();
            _repositoryFactory = () => _repositoryMock.Object;
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _repositoryMock.Setup(ss => ss.UnitOfWork).Returns(_mockUnitOfWork.Object);
            _eventPublisherMock = new Mock<IEventPublisher>();
             var templateLoader = new Mock<INotificationTemplateLoader>();
            _memCache = GetCache();
            _notificationService = new NotificationService(_repositoryFactory, _eventPublisherMock.Object, templateLoader.Object, _memCache);
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchService = new NotificationSearchService(_repositoryFactory, _notificationServiceMock.Object, _memCache);
            _notificationRegistrar = new NotificationRegistrar(null);

            if (!AbstractTypeFactory<NotificationEntity>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(SocialNetworkNotificationEntity)))
                AbstractTypeFactory<NotificationEntity>.RegisterType<SocialNetworkNotificationEntity>();

        }

        //that just samples how to use extensions for notification
        [Fact(Skip = "fail. Temporary disabled. TODO")]
        public async Task GetNotificationByTypeAsync_ReturnNotification()
        {
            //Arrange
            var type = nameof(RegistrationSocialNetworkNotification);

            var mockNotifications = new List<NotificationEntity>().AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
            _notificationRegistrar.RegisterNotification<RegistrationSocialNetworkNotification>();

            //Act
            var result = await _notificationSearchService.GetNotificationAsync(type);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(type, result.Type);
        }

        [Fact]
        public async Task GetNotificationsByIdsAsync_ReturnNotifications()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var notifications = new List<NotificationEntity> { new SocialNetworkNotificationEntity() { Id = id, Type = nameof(SocialNetworkNotification) } };
            var responseGroup = NotificationResponseGroup.Default.ToString();
            _repositoryMock.Setup(n => n.GetByIdsAsync(new[] { id }, responseGroup)).ReturnsAsync(notifications.ToArray());
            var notificationsMock = notifications.AsQueryable().BuildMock();
            _repositoryMock.Setup(x => x.Notifications).Returns(notificationsMock.Object);
            _notificationRegistrar.RegisterNotification<RegistrationSocialNetworkNotification>();

            //Act
            var result = await _notificationService.GetByIdsAsync(new[] { id }, responseGroup);

            //Assert
            Assert.NotNull(result);
            Assert.Contains(result, r => r.Id.Equals(id));
        }

        [Fact(Skip = "fail. Temporary disabled. TODO")]
        public async Task SaveChangesAsync_SavedNotification()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            _notificationRegistrar.RegisterNotification<RegistrationSocialNetworkNotification>();
            var notificationEntities = new List<NotificationEntity>
            {
                new SocialNetworkNotificationEntity()
                {
                    Id = id,
                    Type = nameof(RegistrationSocialNetworkNotification),
                    Kind = nameof(SocialNetworkNotification),
                    Token = Guid.NewGuid().ToString()
                }
            };
            _repositoryMock.Setup(n => n.GetByIdsAsync(new[] { id }, NotificationResponseGroup.Full.ToString()))
                .ReturnsAsync(notificationEntities.ToArray());
            var notificationsMock = notificationEntities.AsQueryable().BuildMock();
            _repositoryMock.Setup(x => x.Notifications).Returns(notificationsMock.Object);
            var notifications = notificationEntities.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type)));

            //Act
            await _notificationService.SaveChangesAsync(notifications.ToArray());
        }

        private static IPlatformMemoryCache GetCache()
        {
            var defaultOptions = Options.Create(new CachingOptions() {});
            var logger = new Moq.Mock<ILogger<PlatformMemoryCache>>();
            return new PlatformMemoryCache(new MemoryCache(new MemoryCacheOptions()), defaultOptions, logger.Object);
        }
    }


}
