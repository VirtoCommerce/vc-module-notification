using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.NotificationsModule.Twilio;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Senders;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Data.TemplateLoaders;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
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
        private readonly Mock<IBackgroundJobClient> _backgroundJobClient;
        

        public NotificationSenderIntegrationTests()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<NotificationSenderIntegrationTests>();

            Configuration = builder.Build();

            _emailSendingOptions = new SmtpSenderOptions()
            {
                SmtpServer = "smtp.gmail.com", // If use smtp.gmail.com then SSL is enabled and check https://www.google.com/settings/security/lesssecureapps
                Port = 587,
                Login = "tasker.for.test@gmail.com",
                Password = "",
                EnableSsl = true
            };
            _templateRender = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions() { CustomFilterTypes = new HashSet<Type> { typeof(UrlFilters), typeof(TranslationFilter) } }));
            _messageServiceMock = new Mock<INotificationMessageService>();
            _emailSendingOptionsMock = new Mock<IOptions<SmtpSenderOptions>>();
            _logNotificationSenderMock = new Mock<ILogger<NotificationSender>>();
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _backgroundJobClient = new Mock<IBackgroundJobClient>();
            _notificationRegistrar = new NotificationRegistrar(_notificationServiceMock.Object, _notificationSearchServiceMock.Object, null, Options.Create(new FileSystemTemplateLoaderOptions()));

            if (!AbstractTypeFactory<NotificationTemplate>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationTemplate)))
                AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>().MapToType<NotificationTemplateEntity>();

            if (!AbstractTypeFactory<NotificationMessage>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationMessage)))
                AbstractTypeFactory<NotificationMessage>.RegisterType<EmailNotificationMessage>().MapToType<NotificationMessageEntity>();

            if (!AbstractTypeFactory<NotificationTemplate>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(SmsNotificationTemplate)))
                AbstractTypeFactory<NotificationTemplate>.RegisterType<SmsNotificationTemplate>().MapToType<NotificationTemplateEntity>();

            if (!AbstractTypeFactory<NotificationMessage>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(SmsNotificationMessage)))
                AbstractTypeFactory<NotificationMessage>.RegisterType<SmsNotificationMessage>().MapToType<NotificationMessageEntity>();

            _notificationRegistrar.RegisterNotification<RegistrationEmailNotification>();
            _notificationRegistrar.RegisterNotification<ResetPasswordEmailNotification>();
            _notificationRegistrar.RegisterNotification<TwoFactorSmsNotification>();

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(null, null));

            _emailSendingOptionsMock.Setup(opt => opt.Value).Returns(_emailSendingOptions);
        }

        public IConfiguration Configuration { get; set; }

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

        [Fact]
        public async Task TwilioNotificationMessageSender_SuccessSentMessage()
        {
            //Arrange
            var notification = GetSmsNotification();
            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message");
            await notification.ToMessageAsync(message, _templateRender);
            var accountId = Configuration["TwilioAccountSID"];
            var accountPassword = Configuration["TwilioAuthToken"];
            var defaultSender = Configuration["TwilioDefaultSender"];

            var twilioSendingOptionsMock = new Mock<IOptions<TwilioSenderOptions>>();
            twilioSendingOptionsMock.Setup(opt => opt.Value).Returns(new TwilioSenderOptions { AccountId = accountId, AccountPassword = accountPassword });
            var smsSendingOptions = new Mock<IOptions<SmsSendingOptions>>();
            smsSendingOptions.Setup(opt => opt.Value).Returns(new SmsSendingOptions { SmsDefaultSender = defaultSender });
            _messageSender = new TwilioSmsNotificationMessageSender(twilioSendingOptionsMock.Object, smsSendingOptions.Object);
            _messageServiceMock.Setup(x => x.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync(new[] { message });

            var notificationSender = GetNotificationSender();
            _notificationMessageSenderProviderFactory.RegisterSenderForType<SmsNotification, TwilioSmsNotificationMessageSender>();

            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        private NotificationSender GetNotificationSender()
        {
            _notificationMessageSenderProviderFactory = new NotificationMessageSenderProviderFactory(new List<INotificationMessageSender>() { _messageSender });
            return new NotificationSender(_templateRender, _messageServiceMock.Object, _logNotificationSenderMock.Object, _notificationMessageSenderProviderFactory, _backgroundJobClient.Object);
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
                TenantIdentity = new TenantIdentity(null, null),
                IsActive = true
            };
        }

        private Notification GetSmsNotification()
        {
            var recipient = Configuration["TwilioRecipient"];
            var message = "test sms";
            return new TwoFactorSmsNotification()
            {
                Number = recipient,
                Templates = new List<NotificationTemplate>()
                {
                    new SmsNotificationTemplate()
                    {
                        Message = message
                    }
                },
                TenantIdentity = new TenantIdentity(null, null),
                IsActive = true
            };
        }
    }
}
