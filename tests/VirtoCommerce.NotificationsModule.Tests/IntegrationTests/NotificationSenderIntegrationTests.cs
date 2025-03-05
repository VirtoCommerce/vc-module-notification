using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Senders;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.NotificationsModule.SendGrid;
using VirtoCommerce.NotificationsModule.Smtp;
using VirtoCommerce.NotificationsModule.Tests.Model;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.NotificationsModule.Twilio;
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
        private readonly Mock<IOptions<SmtpSenderOptions>> _smptpOptionsMock;
        private readonly Mock<IOptions<EmailSendingOptions>> _emailSendingOptions;
        private readonly INotificationRegistrar _notificationRegistrar;
        private readonly Mock<ILogger<NotificationSender>> _logNotificationSenderMock;
        private INotificationMessageSenderFactory _notificationMessageSenderFactory;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly SmtpSenderOptions _smtpOptionsGmail;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;
        private readonly Mock<IBackgroundJobClient> _backgroundJobClient;
        private readonly Mock<INotificationLayoutService> _notificationLayoutServiceMock;
        private readonly Mock<INotificationLayoutSearchService> _notificationLayoutSearchService;
        private readonly Mock<IEmailAttachmentService> _emailAttachmentServiceMock;
        private readonly IHttpClientFactory _httpClientFactory;

        public NotificationSenderIntegrationTests()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<NotificationSenderIntegrationTests>();

            Configuration = builder.Build();

            _httpClientFactory = new Mock<IHttpClientFactory>().Object;
            var httpClient = new HttpClient();
            Mock.Get(_httpClientFactory).Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _smtpOptionsGmail = new SmtpSenderOptions()
            {
                SmtpServer = "smtp.gmail.com", // If use smtp.gmail.com then SSL is enabled and check https://www.google.com/settings/security/lesssecureapps
                Port = 587,
                Login = Configuration["SenderEmail"],
                Password = Configuration["SenderEmailPassword"],
                ForceSslTls = true,
            };

            _notificationLayoutServiceMock = new Mock<INotificationLayoutService>();

            _notificationLayoutSearchService = new Mock<INotificationLayoutSearchService>();
            var notificationLayoutSearchResult = new NotificationLayoutSearchResult() { Results = new List<NotificationLayout>() };
            _notificationLayoutSearchService.Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>())).ReturnsAsync(notificationLayoutSearchResult);

            Func<ITemplateLoader> factory = () => new LayoutTemplateLoader(_notificationLayoutServiceMock.Object);
            _templateRender = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions() { CustomFilterTypes = new HashSet<Type> { typeof(UrlFilters), typeof(TranslationFilter) } }), factory, _notificationLayoutSearchService.Object);
            _messageServiceMock = new Mock<INotificationMessageService>();
            _smptpOptionsMock = new Mock<IOptions<SmtpSenderOptions>>();
            _emailSendingOptions = new Mock<IOptions<EmailSendingOptions>>();
            _logNotificationSenderMock = new Mock<ILogger<NotificationSender>>();
            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _backgroundJobClient = new Mock<IBackgroundJobClient>();
            _notificationRegistrar = new NotificationRegistrar(null);
            _emailAttachmentServiceMock = new Mock<IEmailAttachmentService>();

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

            _smptpOptionsMock.Setup(opt => opt.Value).Returns(_smtpOptionsGmail);
        }

        public IConfiguration Configuration { get; set; }

        [Fact]
        public async Task SmtpEmailNotificationMessageSender_SuccessSentMessage()
        {
            //Arrange
            var notification = GetNotification();

            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message") as EmailNotificationMessage;
            message.From = Configuration["SenderEmail"];
            message.To = Configuration["SenderEmail"];

            _messageServiceMock.Setup(x => x.GetNotificationsMessageByIds(It.IsAny<string[]>()))
               .ReturnsAsync([message]);

            _messageSender = new SmtpEmailNotificationMessageSender(_smptpOptionsMock.Object, _emailSendingOptions.Object, _emailAttachmentServiceMock.Object);

            var notificationSender = GetNotificationSender();

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

            _smtpOptionsGmail.Password = "wrong_password";
            _smptpOptionsMock.Setup(opt => opt.Value).Returns(_smtpOptionsGmail);
            _messageSender = new SmtpEmailNotificationMessageSender(_smptpOptionsMock.Object, _emailSendingOptions.Object, _emailAttachmentServiceMock.Object);

            var notificationSender = GetNotificationSender();

            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task SmtpEmailNotificationMessageSender_WithAttachmentFromUrl()
        {
            //Arrange
            var notification = GetNotification();

            var message = (EmailNotificationMessage)AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message");
            message.From = Configuration["SenderEmail"];
            message.To = Configuration["SenderEmail"];
            message.Subject = "Test subject: email with attachment from URL";
            message.Body = "Test body: email with attachment from URL";

            message.Attachments.Add(new EmailAttachment
            {
                FileName = "test.txt",
                Url = "http://example.com/test.txt",
                MimeType = "text/plain",
            });

            var expectedContent = "Test content";
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedContent)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            httpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var attachmentService = new EmailAttachmentService(httpClientFactoryMock.Object);

            _messageServiceMock
                .Setup(x => x.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync([message]);

            _messageSender = new SmtpEmailNotificationMessageSender(_smptpOptionsMock.Object, _emailSendingOptions.Object, attachmentService);

            var notificationSender = GetNotificationSender();

            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }


        [Fact]
        public async Task SmtpEmailNotificationMessageSender_WithAttachmentFromFile()
        {
            //Arrange
            var notification = GetNotification();

            var message = (EmailNotificationMessage)AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message");
            message.From = Configuration["SenderEmail"];
            message.To = Configuration["SenderEmail"];
            message.Subject = "Test subject: email with attachment from File";
            message.Body = "Test body: email with attachment from File";

            message.Attachments.Add(new EmailAttachment
            {
                FileName = "test.png",
                Url = "IntegrationTests/All spec.png",
                MimeType = "image/png",
            });

            var attachmentService = new EmailAttachmentService(_httpClientFactory);

            _messageServiceMock
                .Setup(x => x.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync([message]);

            _messageSender = new SmtpEmailNotificationMessageSender(_smptpOptionsMock.Object, _emailSendingOptions.Object, attachmentService);

            var notificationSender = GetNotificationSender();

            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SendGridNotificationMessageSender_SuccessSentMessage()
        {
            //Arrange
            var notification = GetNotification();

            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message") as EmailNotificationMessage;
            message.From = Configuration["SendgridSender"];
            message.To = Configuration["SendgridSender"];
            message.Subject = "Subject";
            message.Body = "Message";

            _messageServiceMock.Setup(x => x.GetNotificationsMessageByIds(It.IsAny<string[]>()))
               .ReturnsAsync([message]);

            var sendGridSendingOptionsMock = new Mock<IOptions<SendGridSenderOptions>>();
            sendGridSendingOptionsMock.Setup(opt => opt.Value).Returns(new SendGridSenderOptions { ApiKey = Configuration["SendgridApiKey"] });

            _messageSender = new SendGridEmailNotificationMessageSender(sendGridSendingOptionsMock.Object, _emailSendingOptions.Object, _emailAttachmentServiceMock.Object);

            var notificationSender = GetNotificationSender();

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
            var message = AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{notification.Kind}Message") as SmsNotificationMessage;
            await notification.ToMessageAsync(message, _templateRender);
            message.Number = Configuration["WwilioReciever"];

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

            //Act
            var result = await notificationSender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        private NotificationSender GetNotificationSender()
        {
            _notificationMessageSenderFactory = new NotificationMessageSenderFactory(new List<INotificationMessageSender>() { _messageSender });
            return new NotificationSender(_templateRender, _messageServiceMock.Object, _notificationMessageSenderFactory, _backgroundJobClient.Object);
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
