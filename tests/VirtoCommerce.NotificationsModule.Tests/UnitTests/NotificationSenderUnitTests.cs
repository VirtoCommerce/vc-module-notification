using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Senders;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Data.TemplateLoaders;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.NotificationsModule.Tests.Common;
using VirtoCommerce.NotificationsModule.Tests.Model;
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.NotificationsModule.Web.JsonConverters;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationSenderUnitTests
    {

        private readonly NotificationSender _sender;
        private readonly INotificationTemplateRenderer _templateRender;
        private readonly Mock<INotificationMessageService> _messageServiceMock;
        private readonly Mock<INotificationMessageSender> _messageSenderMock;
        private readonly Mock<ILogger<NotificationSender>> _logNotificationSenderMock;
        private readonly Mock<INotificationMessageSenderProviderFactory> _senderFactoryMock;
        private readonly Mock<IBackgroundJobClient> _backgroundJobClient;

        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;
        private readonly NotificationRegistrar _notificationRegistrar;

        public NotificationSenderUnitTests()
        {
            _templateRender = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions() { CustomFilterTypes = new HashSet<Type> { typeof(UrlFilters), typeof(TranslationFilter) } }));
            _messageServiceMock = new Mock<INotificationMessageService>();
            _messageSenderMock = new Mock<INotificationMessageSender>();
            _logNotificationSenderMock = new Mock<ILogger<NotificationSender>>();

            _senderFactoryMock = new Mock<INotificationMessageSenderProviderFactory>();
            _senderFactoryMock.Setup(s => s.GetSenderForNotificationType(nameof(EmailNotification))).Returns(_messageSenderMock.Object);
            _backgroundJobClient = new Mock<IBackgroundJobClient>();

            _sender = new NotificationSender(_templateRender, _messageServiceMock.Object, _senderFactoryMock.Object, _backgroundJobClient.Object);

            if (!AbstractTypeFactory<NotificationTemplate>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationTemplate)))
                AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>().MapToType<NotificationTemplateEntity>();

            if (!AbstractTypeFactory<NotificationMessage>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(EmailNotificationMessage)))
                AbstractTypeFactory<NotificationMessage>.RegisterType<EmailNotificationMessage>().MapToType<NotificationMessageEntity>();

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(null, null));

            _notificationServiceMock = new Mock<INotificationService>();
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();

            _notificationRegistrar = new NotificationRegistrar(null);
        }

        [Fact]
        public async Task OrderSendEmailNotification_SentNotification()
        {
            //Arrange
            var language = "en-US";
            var subject = "Your order was sent";
            var body = "Your order <strong>{{ customer_order.number}}</strong> was sent.<br> Number of sent parcels - " +
                          "<strong>{{ customer_order.shipments | size}}</strong>.<br> Parcels tracking numbers:<br> {% for shipment in customer_order.shipments %} " +
                          "<br><strong>{{ shipment.number}}</strong> {% endfor %}<br><br>Sent date - <strong>{{ customer_order.modified_date }}</strong>.";
            var notification = new OrderSentEmailNotification()
            {
                CustomerOrder = new CustomerOrder()
                {
                    Number = "123"
                    ,
                    Shipments = new[] { new Shipment() { Number = "some_number" } }
                    ,
                    ModifiedDate = DateTime.Now
                },
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                        LanguageCode = "en-US"
                    }
                },
                LanguageCode = language,
                IsActive = true
            };
            var date = new DateTime(2018, 02, 20, 10, 00, 00);
            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = date
            };

            _messageServiceMock.Setup(ms => ms.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync(new[] { message });

            //Act
            var result = await _sender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task InvoiceEmailNotification_SentNotification()
        {
            //Arrange
            var language = "default";
            var subject = "Invoice for order - <strong>{{ customer_order.number }}</strong>";
            var body = TestUtility.GetStringByPath($"Content\\{nameof(InvoiceEmailNotification)}.html");
            var notification = new InvoiceEmailNotification()
            {
                CustomerOrder = new CustomerOrder()
                {
                    Id = "adsffads",
                    Number = "123",
                    ShippingTotal = 123456.789m,
                    CreatedDate = DateTime.Now,
                    Status = "Paid",
                    Total = 123456.789m,
                    FeeTotal = 123456.789m,
                    SubTotal = 123456.789m,
                    TaxTotal = 123456.789m,
                    Currency = "USD",
                    Items = new[]{ new LineItem
                    {
                        Name = "some",
                        Sku = "sku",
                        PlacedPrice = 12345.6789m,
                        Quantity = 1,
                        ExtendedPrice = 12345.6789m,
                    } }
                },
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                LanguageCode = language,
                IsActive = true
            };
            var date = new DateTime(2018, 02, 20, 10, 00, 00);
            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = date
            };

            _messageServiceMock.Setup(ms => ms.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync(new[] { message });


            //Act
            var result = await _sender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RegistrationEmailNotification_SentNotification()
        {
            //Arrange
            var language = "default";
            var subject = "Your login - {{ login }}.";
            var body = "Thank you for registration {{ firstname }} {{ lastname }}!!!";
            var notification = new RegistrationEmailNotification()
            {
                FirstName = "First Name",
                LastName = "Last Name",
                Login = "Test login",
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                LanguageCode = language,
                IsActive = true
            };
            var date = new DateTime(2018, 02, 20, 10, 00, 00);
            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = date
            };

            _messageServiceMock.Setup(ms => ms.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync(new[] { message });

            //Act
            var result = await _sender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task OrderPaidEmailNotification_SentNotification()
        {
            //Arrange
            var language = "default";
            var subject = "Your order was fully paid";
            var body = "Thank you for paying <strong>{{ customer_order.number }}</strong> order.<br> " +
                          "You had paid <strong>{{ customer_order.total | math.format 'N'}} {{ customer_order.currency }}</strong>.<br>" +
                          " Paid date - <strong>{{ customer_order.modified_date }}</strong>.";
            var notification = new OrderPaidEmailNotification()
            {
                CustomerOrder = new CustomerOrder()
                {
                    Number = "123",
                    Total = 1234.56m,
                    Currency = "USD",
                    ModifiedDate = DateTime.Now
                },
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                LanguageCode = language,
                IsActive = true
            };
            var date = new DateTime(2018, 02, 20, 10, 00, 00);
            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = date
            };

            _messageServiceMock.Setup(ms => ms.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync(new[] { message });

            //Act
            var result = await _sender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task EmailNotification_SuccessSend()
        {
            //Arrange
            string language = null;
            var subject = "some subject";
            var body = "some body";
            var notification = new RegistrationEmailNotification()
            {
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                TenantIdentity = new TenantIdentity(null, null),
                LanguageCode = language,
                IsActive = true
            };

            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = DateTime.Now,
                TenantIdentity = new TenantIdentity(null, null)
            };

            _messageServiceMock.Setup(ms => ms.GetNotificationsMessageByIds(It.IsAny<string[]>()))
                .ReturnsAsync(new [] { message});

            //Act
            var result = await _sender.SendNotificationAsync(notification);

            //Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task EmailNotification_FailSend()
        {
            //Arrange
            var language = "default";
            var subject = "some subject";
            var body = "some body";
            var notification = new RegistrationEmailNotification()
            {
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                LanguageCode = language
            };

            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = DateTime.Now
            };

            _messageServiceMock.Setup(ms => ms.SaveNotificationMessagesAsync(new NotificationMessage[] { message }));
            _messageSenderMock.Setup(ms => ms.SendNotificationAsync(It.IsAny<NotificationMessage>())).Throws(new SmtpException());

            //Act
            var result = await _sender.SendNotificationAsync(notification);

            //Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task EmailNotification_NotActiveNotification_FailSend()
        {
            //Arrange
            var language = "default";
            var subject = "some subject";
            var body = "some body";
            var notification = new RegistrationEmailNotification()
            {
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                LanguageCode = language
            };

            //Act
            var result = await _sender.SendNotificationAsync(notification);

            //Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task EmailNotification_ArgumentNullException()
        {
            //Arrange
            NotificationMessage message = null;
            _messageServiceMock.Setup(ms => ms.SaveNotificationMessagesAsync(new[] { message }));

            //Act
            await Assert.ThrowsAsync<ArgumentNullException>(() => _sender.SendNotificationAsync(null));
        }

        [Fact]
        public async Task SendNotificationAsync_SentEmailNotification()
        {
            //Arrange
            var language = "default";
            var subject = "some subject";
            var body = "some body";
            var notification = new RegistrationEmailNotification()
            {
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                LanguageCode = language
            };

            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = DateTime.Now
            };

            _messageServiceMock.Setup(ms => ms.SaveNotificationMessagesAsync(new NotificationMessage[] { message }));
            _messageSenderMock.Setup(ms => ms.SendNotificationAsync(It.IsAny<NotificationMessage>())).Throws(new SmtpException());

            var sender = new NotificationSender(_templateRender, _messageServiceMock.Object, _senderFactoryMock.Object, _backgroundJobClient.Object);

            //Act
            var result = await sender.SendNotificationAsync(notification);

            //Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ScheduleSendNotification_GetNotification()
        {
            //Arrange
            var type = nameof(SampleEmailNotification);
            var criteria4 = AbstractTypeFactory<NotificationSearchCriteria>.TryCreateInstance();
            criteria4.Take = 1;
            criteria4.NotificationType = type;
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(criteria4)).ReturnsAsync(new NotificationSearchResult());

            _notificationRegistrar.RegisterNotification<SampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
            {
                Subject = "SampleEmailNotification test",
                Body = "SampleEmailNotification body test",
            });
            var notification = AbstractTypeFactory<Notification>.TryCreateInstance(nameof(SampleEmailNotification));
            notification.IsActive = true;
            var jsonSerializeSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { new NotificationPolymorphicJsonConverter() } };
            GlobalConfiguration.Configuration.UseSerializerSettings(jsonSerializeSettings);
            
            //Act
            await _sender.ScheduleSendNotificationAsync(notification);

            //Assert
            Func<Job, bool> condition = job => job.Method.Name == nameof(NotificationSender.TrySendNotificationMessageAsync) && job.Args[0] is null;
            Expression<Func<Job, bool>> expression = a => condition(a);
            _backgroundJobClient.Verify(x => x.Create(It.Is(expression), It.IsAny<EnqueuedState>()));
        }

        [Fact]
        public async Task SendNotification_SetCustomValidationError_NotSend()
        {
            //Arrange
            var language = "default";
            var subject = "some subject";
            var body = "some body";
            var notification = new RegistrationEmailNotification()
            {
                Templates = new List<NotificationTemplate>()
                {
                    new EmailNotificationTemplate()
                    {
                        Subject = subject,
                        Body = body,
                    }
                },
                LanguageCode = language
            };
            notification.SetCustomValidationError("some error");

            var message = new EmailNotificationMessage()
            {
                Id = "1",
                From = "from@from.com",
                To = "to@to.com",
                Subject = subject,
                Body = body,
                SendDate = DateTime.Now
            };

            _messageServiceMock.Setup(ms => ms.SaveNotificationMessagesAsync(new NotificationMessage[] { message }));
            _messageSenderMock.Setup(ms => ms.SendNotificationAsync(It.IsAny<NotificationMessage>())).Throws(new SmtpException());
            _messageServiceMock.Setup(ms => ms.GetNotificationsMessageByIds(It.IsAny<string[]>())).ReturnsAsync(new[] { message })
                .Callback(() => {
                    message.Status = NotificationMessageStatus.Error;
                });

            var sender = GetNotificationSender();

            //Act
            var sendResult = await sender.SendNotificationAsync(notification);

            //Assert
            Assert.False(sendResult.IsSuccess);
            Assert.Equal("Can't send notification message by . There are errors.", sendResult.ErrorMessage);
        }

        private NotificationSender GetNotificationSender()
        {
            return new NotificationSender(_templateRender,
                _messageServiceMock.Object,
                _senderFactoryMock.Object,
                _backgroundJobClient.Object);
        }
    }
}
