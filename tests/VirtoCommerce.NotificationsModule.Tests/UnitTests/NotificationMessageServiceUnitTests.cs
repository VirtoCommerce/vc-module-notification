using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Data.TemplateLoaders;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationMessageServiceUnitTests
    {
        private readonly Mock<INotificationRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;
        private readonly NotificationMessageService _notificationMessageService;

        public NotificationMessageServiceUnitTests()
        {
            _repositoryMock = new Mock<INotificationRepository>();
            Func<INotificationRepository> factory = () => _repositoryMock.Object;
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _repositoryMock.Setup(ss => ss.UnitOfWork).Returns(_mockUnitOfWork.Object);
            _eventPublisherMock = new Mock<IEventPublisher>();
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _notificationMessageService = new NotificationMessageService(factory, _eventPublisherMock.Object);

            if (!AbstractTypeFactory<NotificationMessageEntity>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationMessageEntity)))
                AbstractTypeFactory<NotificationMessageEntity>.RegisterType<EmailNotificationMessageEntity>();

            if (!AbstractTypeFactory<NotificationTemplate>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationTemplate)))
                AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>().MapToType<NotificationTemplateEntity>();

            if (!AbstractTypeFactory<NotificationMessage>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationMessage)))
                AbstractTypeFactory<NotificationMessage>.RegisterType<EmailNotificationMessage>().MapToType<NotificationMessageEntity>();

            var criteria = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            criteria.NotificationType = nameof(RegistrationEmailNotification);
            criteria.Take = 1;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria)).ReturnsAsync(new NotificationSearchResult());
            var notificationService = new NotificationRegistrar(_notificationServiceMock.Object, _notificationSearchServiceMock.Object, null, Options.Create(new FileSystemTemplateLoaderOptions()));
            notificationService.RegisterNotification<RegistrationEmailNotification>();
        }

        [Fact]
        public async Task GetNotificationsMessageByIds_GetMessage()
        {
            //Arrange
            var type = nameof(RegistrationEmailNotification);
            var id = Guid.NewGuid().ToString();
            var notificationId = Guid.NewGuid().ToString();
            var message = new EmailNotificationMessageEntity { Id = id, NotificationType = nameof(EmailNotificationMessage), NotificationId = notificationId };
            var messages = new List<NotificationMessageEntity> { message };
            _repositoryMock.Setup(n => n.GetMessagesByIdsAsync(new[] { id })).ReturnsAsync(messages.ToArray());
            var notification = AbstractTypeFactory<Notification>.TryCreateInstance(type);
            notification.Id = notificationId;
            _notificationServiceMock.Setup(n => n.GetByIdsAsync(new[] { notificationId }, null)).ReturnsAsync(new[] { notification });

            //Act
            var result = await _notificationMessageService.GetNotificationsMessageByIds(new[] { id });

            //Assert
            Assert.NotNull(result);
            Assert.Contains(result, r => r.Id.Equals(id));
        }

        [Fact]
        public async Task SaveNotificationMessages_SaveMessage()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();
            var messages = new List<EmailNotificationMessage>
            {
                new EmailNotificationMessage()
                {
                    Id = id,
                    NotificationId = Guid.NewGuid().ToString(),
                    NotificationType = nameof(RegistrationEmailNotification),
                    From = "test@test.com",
                    To = "test@test.com",
                    Subject = "subj",
                    Body = "body"
                }
            };
            var messageEntity = new EmailNotificationMessageEntity() { Id = id, NotificationType = nameof(EmailNotificationMessage) };
            var messageEntities = new List<NotificationMessageEntity> { messageEntity };
            _repositoryMock.Setup(n => n.GetMessagesByIdsAsync(new[] { id })).ReturnsAsync(messageEntities.ToArray());

            //Act
            await _notificationMessageService.SaveNotificationMessagesAsync(messages.ToArray());
        }
    }
}
