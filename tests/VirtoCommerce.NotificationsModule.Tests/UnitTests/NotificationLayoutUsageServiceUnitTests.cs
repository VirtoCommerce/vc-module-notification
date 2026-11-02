using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Services;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationLayoutUsageServiceUnitTests
    {
        private readonly Mock<INotificationLayoutService> _layoutService = new();
        private readonly List<Notification> _registered = [];
        private readonly List<TestUsageService.Row> _dbTemplates = [];
        private readonly TestUsageService _service;

        public NotificationLayoutUsageServiceUnitTests()
        {
            var registrar = new Mock<INotificationRegistrar>();
            registrar.SetupGet(x => x.AllRegisteredNotifications).Returns(_registered);
            _service = new TestUsageService(_layoutService.Object, registrar.Object, _dbTemplates);
        }

        [Fact]
        public async Task GetUsageAsync_LayoutNotFound_Null()
        {
            SetupLayout(null);

            Assert.Null(await _service.GetUsageAsync("missing"));
        }

        [Fact]
        public async Task GetUsageAsync_PredefinedLayout_CountsPredefinedAndDbTemplatesByNameAndId()
        {
            SetupLayout(new NotificationLayout { Id = "4f1c2d", Name = "Default", IsPredefined = true });
            Register("OrderCreateEmailNotification", ("en-US", "Default"));
            Register("OrderPaidEmailNotification", ("en-US", "Default"));
            Register("ResetPasswordEmailNotification", ("en-US", null));
            _dbTemplates.Add(new("RegistrationEmailNotification", null, "en-US", "4f1c2d"));
            _dbTemplates.Add(new("InvoiceEmailNotification", "store-1", "en-US", "default"));

            var usage = await _service.GetUsageAsync("Default");

            Assert.Equal("4f1c2d", usage.LayoutId);
            Assert.Equal(["InvoiceEmailNotification", "OrderCreateEmailNotification", "OrderPaidEmailNotification", "RegistrationEmailNotification"], usage.NotificationTypes);
            Assert.Equal(4, usage.Count);
        }

        [Fact]
        public async Task GetUsageAsync_PredefinedTemplateOverriddenInDb_DbTemplateDecides()
        {
            SetupLayout(new NotificationLayout { Id = "Default", Name = "Default", IsPredefined = true });
            Register("OrderCreateEmailNotification", ("en-US", "Default"), ("de-DE", "Default"));
            Register("OrderPaidEmailNotification", ("en-US", "Default"));
            // Order paid was edited in the DB without the layout; order created only in German
            _dbTemplates.Add(new("OrderPaidEmailNotification", null, "en-US", null));
            _dbTemplates.Add(new("OrderCreateEmailNotification", null, "de-DE", null));

            var usage = await _service.GetUsageAsync("Default");

            Assert.Equal(["OrderCreateEmailNotification"], usage.NotificationTypes);
        }

        [Fact]
        public async Task GetUsageAsync_DefaultLayout_CountsTemplatesWithoutLayout()
        {
            SetupLayout(new NotificationLayout { Id = "b7e9", Name = "Corporate", IsDefault = true });
            Register("ResetPasswordEmailNotification", ("en-US", null));
            Register("OrderCreateEmailNotification", ("en-US", "Default"));
            _dbTemplates.Add(new("RegistrationEmailNotification", null, "en-US", ""));

            var usage = await _service.GetUsageAsync("b7e9");

            Assert.Equal(["RegistrationEmailNotification", "ResetPasswordEmailNotification"], usage.NotificationTypes);
        }

        [Fact]
        public async Task GetUsageAsync_SmsTemplates_Ignored()
        {
            SetupLayout(new NotificationLayout { Id = "b7e9", Name = "Corporate", IsDefault = true });
            _registered.Add(new SmsTestNotification { Templates = [new SmsNotificationTemplate { LanguageCode = "en-US" }] });

            var usage = await _service.GetUsageAsync("b7e9");

            Assert.Empty(usage.NotificationTypes);
        }

        private void SetupLayout(NotificationLayout layout)
        {
            _layoutService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(layout == null ? [] : new List<NotificationLayout> { layout });
        }

        private void Register(string type, params (string Language, string LayoutId)[] templates)
        {
            var notification = new EmailTestNotification(type) { Templates = [] };
            foreach (var (language, layoutId) in templates)
            {
                notification.Templates.Add(new EmailNotificationTemplate { LanguageCode = language, NotificationLayoutId = layoutId, IsPredefined = true });
            }
            _registered.Add(notification);
        }

        private sealed class EmailTestNotification : EmailNotification
        {
            public EmailTestNotification(string type) : base(type)
            {
            }
        }

        private sealed class SmsTestNotification : SmsNotification
        {
            public SmsTestNotification() : base("SmsTestNotification")
            {
            }
        }

        private sealed class TestUsageService(INotificationLayoutService layoutService, INotificationRegistrar registrar, IList<TestUsageService.Row> rows)
            : NotificationLayoutUsageService(() => Mock.Of<INotificationRepository>(), layoutService, registrar)
        {
            public sealed record Row(string Type, string TenantId, string LanguageCode, string NotificationLayoutId);

            protected override Task<IList<DbTemplateInfo>> GetDbTemplatesAsync()
            {
                IList<DbTemplateInfo> result = rows
                    .Select(x => new DbTemplateInfo { Type = x.Type, TenantId = x.TenantId, LanguageCode = x.LanguageCode, NotificationLayoutId = x.NotificationLayoutId })
                    .ToList();
                return Task.FromResult(result);
            }
        }
    }
}
