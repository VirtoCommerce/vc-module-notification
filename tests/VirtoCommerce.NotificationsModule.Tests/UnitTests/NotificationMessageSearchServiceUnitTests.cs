using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests;

public class NotificationMessageSearchServiceUnitTests
{
    private readonly Mock<INotificationRepository> _repositoryMock;
    private readonly Mock<INotificationMessageService> _messageServiceMock;
    private readonly NotificationMessageSearchService _searchService;

    public NotificationMessageSearchServiceUnitTests()
    {
        _repositoryMock = new Mock<INotificationRepository>();
        _messageServiceMock = new Mock<INotificationMessageService>();
        _searchService = new NotificationMessageSearchService(() => _repositoryMock.Object, _messageServiceMock.Object);

        if (!AbstractTypeFactory<EmailNotificationMessage>
                .AllTypeInfos
                .SelectMany(x => x.AllSubclasses)
                .Contains(typeof(EmailNotificationMessage)))
        {
            AbstractTypeFactory<EmailNotificationMessage>.RegisterType<EmailNotificationMessage>();
        }
    }

    [Fact]
    public async Task SearchMessageAsync_ShouldFilterByTenantIdAndType()
    {
        // Arrange
        var tenantId = "tenantId1";
        var tenantType = "Store";
        var searchCriteria = new NotificationMessageSearchCriteria { ObjectIds = new[] { tenantId }, ObjectTypes = new[] { tenantType }, Take = 10, Skip = 0 };
        var notificationEntities = new List<NotificationMessageEntity> { new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString(), TenantId = tenantId, TenantType = tenantType } };

        _repositoryMock.Setup(r => r.NotificationMessages).Returns(notificationEntities.AsQueryable().BuildMock());
        _messageServiceMock.Setup(m => m.GetNotificationsMessageByIds(It.IsAny<string[]>()))
            .ReturnsAsync(notificationEntities.Select(n =>
                    n.ToModel(AbstractTypeFactory<EmailNotificationMessage>.TryCreateInstance(n.NotificationType)))
                .ToArray());

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
        var searchCriteria =
            new NotificationMessageSearchCriteria { NotificationType = notificationType, Take = 10, Skip = 0 };

        var notificationEntities = new List<NotificationMessageEntity> { new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString(), NotificationType = notificationType } };

        _repositoryMock.Setup(r => r.NotificationMessages)
            .Returns(notificationEntities.AsQueryable().BuildMock());
        _messageServiceMock.Setup(m => m.GetNotificationsMessageByIds(It.IsAny<string[]>()))
            .ReturnsAsync(notificationEntities.Select(n =>
                    n.ToModel(AbstractTypeFactory<EmailNotificationMessage>.TryCreateInstance(n.NotificationType)))
                .ToArray());
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
        var searchCriteria = new NotificationMessageSearchCriteria { Keyword = keyword, Take = 10, Skip = 0 };

        var notificationEntities = new List<NotificationMessageEntity>
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
                LastSendError = keyword
            }
        };

        _repositoryMock.Setup(r => r.NotificationMessages)
            .Returns(notificationEntities.AsQueryable().BuildMock());
        _messageServiceMock.Setup(m => m.GetNotificationsMessageByIds(It.IsAny<string[]>()))
            .ReturnsAsync(notificationEntities.Select(n =>
                    n.ToModel(AbstractTypeFactory<EmailNotificationMessage>.TryCreateInstance(n.NotificationType)))
                .ToArray());
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
        var searchCriteria = new NotificationMessageSearchCriteria
        {
            Sort = $"{nameof(NotificationMessageEntity.CreatedDate)}:desc", // Setting Sort instead of SortInfos
            Take = 10,
            Skip = 0
        };

        var notificationEntities = new List<NotificationMessageEntity> { new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString(), CreatedDate = DateTime.UtcNow.AddDays(-1) }, new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString(), CreatedDate = DateTime.UtcNow } };

        _repositoryMock.Setup(r => r.NotificationMessages)
            .Returns(notificationEntities.AsQueryable().BuildMock());
        _messageServiceMock.Setup(m => m.GetNotificationsMessageByIds(It.IsAny<string[]>()))
            .ReturnsAsync(notificationEntities.Select(n =>
                    n.ToModel(AbstractTypeFactory<EmailNotificationMessage>.TryCreateInstance(n.NotificationType)))
                .ToArray());
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
        var searchCriteria = new NotificationMessageSearchCriteria { Take = 1, Skip = 1 };

        var notificationEntities = new List<NotificationMessageEntity> { new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString() }, new EmailNotificationMessageEntity { Id = Guid.NewGuid().ToString() } };

        _repositoryMock.Setup(r => r.NotificationMessages).Returns(notificationEntities.AsQueryable().BuildMock());
        _messageServiceMock.Setup(m => m.GetNotificationsMessageByIds(It.IsAny<string[]>()))
            .ReturnsAsync(notificationEntities.Skip(1).Take(1).Select(n => n.ToModel(AbstractTypeFactory<EmailNotificationMessage>.TryCreateInstance(n.NotificationType))).ToArray());

        // Act
        var result = await _searchService.SearchMessageAsync(searchCriteria);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Results);
        Assert.Equal(notificationEntities[1].Id, result.Results.First().Id);
    }
}
