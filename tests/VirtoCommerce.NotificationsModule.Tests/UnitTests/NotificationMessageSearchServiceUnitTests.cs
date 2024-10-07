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
        var tenantId = "tenantId1";
        var tenantType = "Store";

        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                TenantType = tenantType,
            }
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
        Assert.Single(result.Results);
        Assert.Equal(tenantId, result.Results.First().TenantIdentity.Id);
        Assert.Equal(tenantType, result.Results.First().TenantIdentity.Type);
    }

    [Fact]
    public async Task SearchMessageAsync_ShouldFilterByNotificationType()
    {
        // Arrange
        var notificationType = "RegistrationEmailNotification";

        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString(), NotificationType = notificationType },
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
        Assert.Single(result.Results);
        Assert.Equal(notificationType, result.Results.First().NotificationType);
    }

    [Theory]
    [InlineData("keyword1")]
    [InlineData("keyword2")]
    public async Task SearchMessageAsync_ShouldFilterByKeyword(string keyword)
    {
        // Arrange
        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity
            {
                Id = Guid.NewGuid().ToString(),
                Subject = keyword,
                Body = keyword,
                From = keyword,
                To = keyword,
                CC = keyword,
                BCC = keyword,
                LastSendError = keyword,
            },
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
        var firstResult = (EmailNotificationMessage)result.Results.First();
        Assert.NotNull(result);
        Assert.Single(result.Results);
        Assert.Contains(keyword, firstResult.Subject);
        Assert.Contains(keyword, firstResult.Body);
        Assert.Contains(keyword, firstResult.From);
        Assert.Contains(keyword, firstResult.To);
        Assert.Contains(keyword, firstResult.CC);
        Assert.Contains(keyword, firstResult.BCC);
        Assert.Contains(keyword, firstResult.LastSendError);
    }

    [Fact]
    public async Task SearchMessageAsync_ShouldSortByCreatedDateDescending()
    {
        // Arrange
        var entities = new List<NotificationMessageEntity>
        {
            new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString(), CreatedDate = new DateTime(2023,1,1) },
            new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString(), CreatedDate = new DateTime(2024,1,1) },
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
            .Returns(entities.AsQueryable().BuildMock());

        _repositoryMock
            .Setup(x => x.GetMessagesByIdsAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync((IList<string> ids) => entities.Where(x => ids.Contains(x.Id)).ToList());
    }
}
