using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Senders;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.SendGrid;
using VirtoCommerce.NotificationsModule.Smtp;
using VirtoCommerce.NotificationsModule.Tests.Model;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.IntegrationTests
{
    [Trait("Category", "IntegrationTest")]
    public class NotificationSenderIntegrationTests
    {
        private INotificationMessageSender _messageSender;
        private readonly INotificationTemplateRenderer _templateRender;
        private readonly Mock<INotificationMessageService> _messageServiceMock;
        private readonly Mock<IOptions<SmtpSenderOptions>> _emailSendingOptionsMock;
        private readonly INotificationRegistrar _notificationRegistrar;
        private readonly Mock<ILogger<NotificationSender>> _logNotificationSenderMock;
        private INotificationMessageSenderProviderFactory _notificationMessageSenderProviderFactory;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly SmtpSenderOptions _emailSendingOptions;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;

        public NotificationSenderIntegrationTests()
        {
            _emailSendingOptions = new SmtpSenderOptions()
            {
                SmtpServer = "smtp.gmail.com", // If use smtp.gmail.com then SSL is enabled and check https://www.google.com/settings/security/lesssecureapps
                Port = 587,
                Login = "tasker.for.test@gmail.com",
                Password = "",
                EnableSsl = true
            };
            _templateRender = new LiquidTemplateRenderer();
            _messageServiceMock = new Mock<INotificationMessageService>();
            _emailSendingOptionsMock = new Mock<IOptions<SmtpSenderOptions>>();
            _logNotificationSenderMock = new Mock<ILogger<NotificationSender>>();
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _notificationRegistrar = new NotificationRegistrar(_notificationServiceMock.Object, _notificationSearchServiceMock.Object);

            if (!AbstractTypeFactory<NotificationTemplate>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationTemplate)))
                AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>().MapToType<NotificationTemplateEntity>();

            if (!AbstractTypeFactory<NotificationMessage>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationMessage)))
                AbstractTypeFactory<NotificationMessage>.RegisterType<EmailNotificationMessage>().MapToType<NotificationMessageEntity>();

            _notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();
            _notificationRegistrar.RegisterNotification<ResetPasswordEmailNotification>();

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(null, null));

            _emailSendingOptionsMock.Setup(opt => opt.Value).Returns(_emailSendingOptions);
        }

        [Fact]
        public async Task SmtpEmailNotificationMessageSender_SuccessSentMessage()
        {
            //Arrange
            var notification = GetNotification();

            _messageSender = new SmtpEmailNotificationMessageSender(_emailSendingOptionsMock.Object);

            var notificationSender = GetNotificationSender();
            _notificationMessageSenderProviderFactory.RegisterSenderForType<EmailNotification, SmtpEmailNotificationMessageSender>();

            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SmtpEmailNotificationMessageSender_FailSendMessage()
        {
            //Arrange
            var notification = GetNotification();

            _emailSendingOptions.Password = "wrong_password";
            _emailSendingOptionsMock.Setup(opt => opt.Value).Returns(_emailSendingOptions);
            _messageSender = new SmtpEmailNotificationMessageSender(_emailSendingOptionsMock.Object);

            var notificationSender = GetNotificationSender();
            _notificationMessageSenderProviderFactory.RegisterSenderForType<EmailNotification, SmtpEmailNotificationMessageSender>();


            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task SendGridNotificationMessageSender_SuccessSentMessage()
        {
            //Arrange
            var notification = GetNotification();

            var sendGridSendingOptionsMock = new Mock<IOptions<SendGridSenderOptions>>();
            sendGridSendingOptionsMock.Setup(opt => opt.Value).Returns(new SendGridSenderOptions { ApiKey = "" });
            _messageSender = new SendGridEmailNotificationMessageSender(sendGridSendingOptionsMock.Object);

            var notificationSender = GetNotificationSender();
            _notificationMessageSenderProviderFactory.RegisterSenderForType<EmailNotification, SendGridEmailNotificationMessageSender>();

            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        private NotificationSender GetNotificationSender()
        {
            _notificationMessageSenderProviderFactory = new NotificationMessageSenderProviderFactory(new List<INotificationMessageSender>() { _messageSender });
            return new NotificationSender(_templateRender, _messageServiceMock.Object, _logNotificationSenderMock.Object, _notificationMessageSenderProviderFactory);
        }

        private Notification GetNotification()
        {
            var number = Guid.NewGuid().ToString();
            var subject = "Order #{{customer_order.number}}";
            var body = "You have order #{{customer_order.number}}";
            return new OrderSentEmailNotification()
            {
                CustomerOrder = new CustomerOrder() { Number = number },
                From = "tasker.for.test@gmail.com",
                To = "tasker.for.test@gmail.com",
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                TenantIdentity = new TenantIdentity(null, null)
            };
        }
    }
}
