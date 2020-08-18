using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MockQueryable.Moq;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Data.TemplateLoaders;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
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
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;
        private readonly IPlatformMemoryCache _memCache;
        private readonly Mock<ICacheEntry> _cacheEntryMock;
        private readonly NotificationSearchService _notificationSearchService;

        public NotificationSearchServiceUnitTests()
        {
            _repositoryMock = new Mock<INotificationRepository>();
            _repositoryFactoryMock = new Mock<Func<INotificationRepository>>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _repositoryFactory = () => _repositoryMock.Object;
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _memCache = GetCache();
            _cacheEntryMock = new Mock<ICacheEntry>();
            _notificationRegistrar = new NotificationRegistrar(_notificationServiceMock.Object, _notificationSearchServiceMock.Object, null, Options.Create(new FileSystemTemplateLoaderOptions()));
            _notificationSearchService = new NotificationSearchService(_repositoryFactory, _notificationServiceMock.Object, _memCache);

         
            var criteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            criteria.Take = 1;
            criteria.NotificationType = nameof(InvoiceEmailNotification);
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.RegisterNotification<InvoiceEmailNotification>();
          
            criteria.NotificationType = nameof(OrderSentEmailNotification);
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.RegisterNotification<OrderSentEmailNotification>();
        
            criteria.NotificationType = nameof(OrderPaidEmailNotification);
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.RegisterNotification<OrderPaidEmailNotification>();
          
            criteria.NotificationType = nameof(RemindUserNameEmailNotification);
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.RegisterNotification<RemindUserNameEmailNotification>();
         
            criteria.NotificationType = nameof(RegistrationEmailNotification);
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();
           

        }

        [Fact]
        public async Task GetNotificationByTypeAsync_ReturnNotification()
        {
            //Arrange
            var type = nameof(RegistrationEmailNotification);

            var notifications = new List<NotificationEntity>
            {
                new EmailNotificationEntity
                {
                    Type = nameof(RegistrationEmailNotification), Kind = nameof(EmailNotification),
                    Id = Guid.NewGuid().ToString(), IsActive = true,
                }
            };

            var mockNotifications = notifications.AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
            _notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();
            var ids = notifications.Select(n => n.Id).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(ids, null))
                .ReturnsAsync(notifications.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray());

            //Act
            var result = await _notificationSearchService.GetNotificationAsync(type);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(type, result.Type);
        }

        [Fact]
        public async Task GetNotificationByAliasAsync_ReturnNotification()
        {
            //Arrange
            var type = "RemindUserNameNotification";

            var notifications = new List<NotificationEntity>
            {
                new EmailNotificationEntity
                {
                    Type = nameof(RemindUserNameEmailNotification), Kind = nameof(EmailNotification),
                    Id = Guid.NewGuid().ToString(), IsActive = true,
                }
            };

            var criteria4 = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            criteria4.Take = 1;
            criteria4.NotificationType = type;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria4)).ReturnsAsync(new NotificationSearchResult());
           
            var mockNotifications = notifications.AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
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
        public async Task SearchNotificationsAsync_GetOneItem()
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

            searchCriteria.Take = 1;
            searchCriteria.Skip = 0;
            var mockNotifications = notifications.AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
            var ids = notifications.Select(n => n.Id).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(ids, null))
                .ReturnsAsync(notifications.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray());
            

            //Act
            var result = await _notificationSearchService.SearchNotificationsAsync(searchCriteria);

            //Assert
            Assert.NotEmpty(result.Results);
            Assert.Single(result.Results);
            Assert.Equal(1, result.Results.Count(r => r.IsActive));
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
            var mockNotifications = notificationEntities.AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
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
        public async Task SearchNotificationsAsync_PagingNotifications(int skip, int take, int expectedCount, string[] expectedTypes)
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
            var mockNotifications = notificationEntities.AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
            var notifications = notificationEntities.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(It.IsAny<string[]>(), responseGroup))
                .ReturnsAsync(notifications.Where(n => expectedTypes.Contains(n.Type)).ToArray());
          
            //Act
            var result = await _notificationSearchService.SearchNotificationsAsync(searchCriteria);

            //Assert
            Assert.Equal(expectedTypes, result.Results.Where(x => x.IsActive).Select(y => y.Type));
            Assert.Equal(expectedCount, result.Results.Count(x => x.IsActive));
        }

        [Fact]
        public async Task GetNotificationAsync_GetByTenant()
        {
            //Arrange
            var searchTenant = new TenantIdentity(Guid.NewGuid().ToString(), "Store");
            var searchType = nameof(RegistrationEmailNotification);
            var searchCriteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            searchCriteria.Take = 1;
            searchCriteria.Skip = 0;
            searchCriteria.TenantId = searchTenant.Id;
            searchCriteria.TenantType = searchTenant.Type;
            searchCriteria.NotificationType = searchType;
            var notificationEntities = new List<NotificationEntity> {
                new EmailNotificationEntity { Type  = searchType, Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), TenantId = null, TenantType = null },
                new EmailNotificationEntity { Type  = searchType, Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), TenantId = "someId", TenantType = "Store" },
                new EmailNotificationEntity { Type  = searchType, Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString(), TenantId = searchTenant.Id, TenantType = searchTenant.Type }
            };
            var mockNotifications = notificationEntities.AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
            var notifications = notificationEntities.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(It.IsAny<string[]>(), null))
                .ReturnsAsync(notifications.ToArray());
          

            //Act
            var result = await NotificationSearchServiceExtensions.GetNotificationAsync<RegistrationEmailNotification>(_notificationSearchService, searchTenant);

            //Assert
            Assert.Equal(searchTenant.Id, result.TenantIdentity.Id);
            Assert.Equal(searchTenant.Type, result.TenantIdentity.Type);
            Assert.Equal(searchType, result.Type);
        }


        [Fact]
        public async Task SearchNotificationsAsync_GetExtendedNotificationWithBaseType()
        {
            //Arrange
            var baseType = nameof(SampleEmailNotification);
            var extendedType = nameof(ExtendedSampleEmailNotification);
            var searchCriteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            searchCriteria.NotificationType = baseType;
            searchCriteria.Take = 1;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(searchCriteria)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.RegisterNotification<SampleEmailNotification>();
            searchCriteria.NotificationType = extendedType;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(searchCriteria)).ReturnsAsync(new NotificationSearchResult());
            _notificationRegistrar.OverrideNotificationType<SampleEmailNotification, ExtendedSampleEmailNotification>();

            var sampleNotificationEntity = new EmailNotificationEntity { Type = baseType, Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString() };
            var notificationEntities = new List<NotificationEntity> {
                sampleNotificationEntity,
                new EmailNotificationEntity { Type  = baseType, Kind = nameof(EmailNotification), Id = Guid.NewGuid().ToString() }
            };
            var mockNotifications = notificationEntities.AsQueryable().BuildMock();
            _repositoryMock.Setup(r => r.Notifications).Returns(mockNotifications.Object);
            var notifications = notificationEntities.Select(n => n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type))).ToArray();
            _notificationServiceMock.Setup(ns => ns.GetByIdsAsync(It.IsAny<string[]>(), searchCriteria.ResponseGroup))
                .ReturnsAsync(notifications.Where(x => x.Id.EqualsInvariant(sampleNotificationEntity.Id)).ToArray());
           

            //Act
            var result = (await _notificationSearchService.SearchNotificationsAsync(searchCriteria)).Results.FirstOrDefault();

            //Assert
            Assert.Equal(baseType, result.Type);
        }

        public class PagingTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { 0, 20, 3, new[] { nameof(InvoiceEmailNotification), nameof(OrderSentEmailNotification), nameof(RegistrationEmailNotification) } };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private static IPlatformMemoryCache GetCache()
        {
            var defaultOptions = Options.Create(new CachingOptions() { CacheSlidingExpiration = TimeSpan.FromMilliseconds(10) });
            var logger = new Moq.Mock<ILogger<PlatformMemoryCache>>();
            return new PlatformMemoryCache(new MemoryCache(new MemoryCacheOptions()), defaultOptions, logger.Object);
        }
    }
}
