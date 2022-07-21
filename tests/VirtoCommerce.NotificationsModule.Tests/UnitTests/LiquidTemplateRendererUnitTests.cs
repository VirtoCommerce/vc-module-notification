using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Scriban.Runtime;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.NotificationsModule.Tests.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Localizations;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class LiquidTemplateRendererUnitTests
    {
        private readonly Mock<ITranslationService> _translationServiceMock;
        private readonly Mock<IBlobUrlResolver> _blobUrlResolverMock;
        private readonly Mock<ICrudService<NotificationLayout>> _notificationLayoutServiceMock;
        private readonly LiquidTemplateRenderer _liquidTemplateRenderer;

        public LiquidTemplateRendererUnitTests()
        {
            _translationServiceMock = new Mock<ITranslationService>();
            _blobUrlResolverMock = new Mock<IBlobUrlResolver>();
            _notificationLayoutServiceMock = new Mock<ICrudService<NotificationLayout>>();

            Func<ITemplateLoader> factory = () => new LayoutTemplateLoader(_notificationLayoutServiceMock.Object);
            _liquidTemplateRenderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions() { CustomFilterTypes = new HashSet<Type> { typeof(UrlFilters), typeof(TranslationFilter) } }), factory);

            //TODO
            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(_translationServiceMock.Object, _blobUrlResolverMock.Object));
            else AbstractTypeFactory<NotificationScriptObject>.OverrideType<NotificationScriptObject, NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(_translationServiceMock.Object, _blobUrlResolverMock.Object));
        }

        [Theory, MemberData(nameof(TranslateData))]
        public async Task Render_TranslateEnglish(string language, JObject jObject)
        {
            //Arrange
            _translationServiceMock.Setup(x => x.GetTranslationDataForLanguage(language)).Returns(jObject);
            var context = new NotificationRenderContext
            {
                Template = "{{ 'order.subject1' | translate: \"" + language + "\" }} test",
            };

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(context);

            //Assert
            Assert.Equal("subj test", result);
        }

        [Fact]
        public async Task Render_LanguageIsEmptyString()
        {
            //Arrange
            var obj = new LiquidTestNotification() { Order = new CustomerOrder { Number = "123", Status = "New" } };
            string body = "Your order 123 changed status to New.";
            var jObject = JObject.FromObject(new { body });
            _translationServiceMock.Setup(x => x.GetTranslationDataForLanguage(string.Empty)).Returns(jObject);

            var context = new NotificationRenderContext
            {
                Template = "{{ 'body' | translate }}",
                Model = obj,
                Language = string.Empty,
            };

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(context);

            //Assert
            Assert.Equal("Your order 123 changed status to New.", result);
        }

        [Fact]
        public async Task Render_GetCurrentYear()
        {
            //Arrange
            var context = new NotificationRenderContext
            {
                Template = "{{created_date | date.to_string '%Y' }}",
                Model = new { CreatedDate = DateTime.Now },
            };

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(context);

            //Assert
            Assert.Equal(DateTime.Now.Year.ToString(), result);
        }

        [Fact]
        public async Task Render_GetAssetUrl()
        {
            //Arrange
            _blobUrlResolverMock.Setup(x => x.GetAbsoluteUrl(It.IsAny<string>())).Returns("http://localhost:10645/assets/1.svg");
            var context = new NotificationRenderContext
            {
                Template = "test {{ '1.svg' | asset_url }}",
            };

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(context);

            //Assert
            Assert.Equal("test http://localhost:10645/assets/1.svg", result);
        }


        public static IEnumerable<object[]> TranslateData
        {
            get
            {
                return new[]
                {
                    new object[] { "en", JObject.FromObject(new { order = new { subject1 = "subj" } }) },
                    new object[] { null, JObject.FromObject(new { order = new { subject1 = "subj" } }) }
                };
            }
        }
    }

    public class LiquidTestNotification
    {
        public string RecipientOutletId { get; set; }
        public string RecipientUserId { get; set; }
        public string RecipientUserName { get; set; }
        public string RecipientLanguage { get; set; }
        public CustomerOrder Order { get; set; }
    }
}
