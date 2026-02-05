using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests;

public class NotificationMessageSearchServiceUnitTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly NotificationMessageSearchService _searchService;

    public NotificationMessageSearchServiceUnitTests()
    {
        AbstractTypeFactory<NotificationMessage>.RegisterType<EmailNotificationMessage>();

        _repositoryMock = new Mock<INotificationRepository>();
        var messageService = new NotificationMessageService(() => _repositoryMock.Object, new Mock<IEventPublisher>().Object);
        _searchService = new NotificationMessageSearchService(() => _repositoryMock.Object, messageService);
    }

    [Fact]
    public async Task SearchMessageAsync_ShouldFilterByTenantIdAndType()
    {
        // Arrange
        const string tenantId = "tenantId1";
        const string tenantType = "Store";

        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                TenantType = tenantType,
            },
            new EmailNotificationMessageEntity
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                TenantType = Guid.NewGuid().ToString(),
            },
        };

        SetupRepository(entities);

        var searchCriteria = new NotificationMessageSearchCriteria
        {
            ObjectIds = [tenantId],
            ObjectTypes = [tenantType],
            Skip = 0,
            Take = 10,
        };

        // Act
        var result = await _searchService.SearchMessageAsync(searchCriteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Results);
        var firstResult = result.Results.First();
        Assert.Equal(tenantId, firstResult.TenantIdentity.Id);
        Assert.Equal(tenantType, firstResult.TenantIdentity.Type);
    }

    [Fact]
    public async Task SearchMessageAsync_ShouldFilterByNotificationType()
    {
        // Arrange
        const string notificationType = "RegistrationEmailNotification";

        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity {
                Id = Guid.NewGuid().ToString(),
                NotificationType = notificationType,
            },
            new EmailNotificationMessageEntity
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                TenantType = Guid.NewGuid().ToString(),
            },
        };

        SetupRepository(entities);

        var searchCriteria = new NotificationMessageSearchCriteria
        {
            NotificationType = notificationType,
            Skip = 0,
            Take = 10,
        };

        // Act
        var result = await _searchService.SearchMessageAsync(searchCriteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Results);
        var firstResult = result.Results.First();
        Assert.Equal(notificationType, firstResult.NotificationType);
    }

    [Theory]
    [InlineData(nameof(EmailNotificationMessageEntity.Subject))]
    [InlineData(nameof(EmailNotificationMessageEntity.Body))]
    [InlineData(nameof(EmailNotificationMessageEntity.From))]
    [InlineData(nameof(EmailNotificationMessageEntity.To))]
    [InlineData(nameof(EmailNotificationMessageEntity.CC))]
    [InlineData(nameof(EmailNotificationMessageEntity.BCC))]
    [InlineData(nameof(EmailNotificationMessageEntity.LastSendError))]
    public async Task SearchMessageAsync_ShouldFilterByKeyword(string propertyName)
    {
        // Arrange
        var keyword = Guid.NewGuid().ToString();
        var entity = new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString() };
        SetProperty(entity, propertyName, $"start_{keyword}_end");

        var entities = new List<NotificationMessageEntity>
        {
            entity,
            new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString() },
            new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString() },
        };

        SetupRepository(entities);

        var searchCriteria = new NotificationMessageSearchCriteria
        {
            Keyword = keyword,
            Skip = 0,
            Take = 10,
        };

        // Act
        var result = await _searchService.SearchMessageAsync(searchCriteria);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Results);
        var firstResult = (EmailNotificationMessage)result.Results.First();
        Assert.NotNull(result);
        Assert.Single(result.Results);

        var fieldValue = firstResult.GetType().GetProperty(propertyName)?.GetValue(firstResult, null);
        switch (fieldValue)
        {
            case string stringValue:
                Assert.Contains(keyword, stringValue);
                break;
            case string[] stringArray:
                Assert.Contains(stringArray, item => item.Contains(keyword));
                break;
            default:
                Assert.Fail($"Unsupported property type for field: {propertyName}");
                break;
        }
    }

    [Fact]
    public async Task SearchMessageAsync_ShouldSortByCreatedDateDescending()
    {
        // Arrange
        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity
            {
                Id = Guid.NewGuid().ToString(), CreatedDate = new DateTime(2023, 1, 1)
            },
            new EmailNotificationMessageEntity
            {
                Id = Guid.NewGuid().ToString(), CreatedDate = new DateTime(2024, 1, 1)
            },
        };

        SetupRepository(entities);

        var searchCriteria = new NotificationMessageSearchCriteria
        {
            Sort = $"{nameof(NotificationMessageEntity.CreatedDate)}:desc",
            Skip = 0,
            Take = 10,
        };

        // Act
        var result = await _searchService.SearchMessageAsync(searchCriteria);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Results.Count);
        Assert.True(result.Results.First().CreatedDate > result.Results.Last().CreatedDate);
    }

    [Fact]
    public async Task SearchMessageAsync_ShouldRespectSkipAndTake()
    {
        // Arrange
        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity { Id = "1" },
            new EmailNotificationMessageEntity { Id = "2" },
            new EmailNotificationMessageEntity { Id = "3" },
        };

        SetupRepository(entities);

        var searchCriteria = new NotificationMessageSearchCriteria
        {
            Sort = nameof(NotificationMessageEntity.Id),
            Skip = 1,
            Take = 1,
        };

        // Act
        var result = await _searchService.SearchMessageAsync(searchCriteria);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Results);
        Assert.Equal("2", result.Results.Single().Id);
    }

    private void SetupRepository(List<NotificationMessageEntity> entities)
    {
        _repositoryMock
            .Setup(x => x.NotificationMessages)
            .Returns(entities.BuildMock());

        _repositoryMock
            .Setup(x => x.GetMessagesByIdsAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync((IList<string> ids) => entities.Where(x => ids.Contains(x.Id)).ToList());
    }

    private static void SetProperty(object entity, string propertyName, object value)
    {
        var propertyInfo = entity.GetType().GetProperty(propertyName);

        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            propertyInfo.SetValue(entity, value);
        }
        else
        {
            throw new ArgumentException($"Property {propertyName} not found or not writable on {entity.GetType().Name}");
        }
    }
}
