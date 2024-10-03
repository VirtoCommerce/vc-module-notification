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
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.NotificationsModule.Tests.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Localizations;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class LiquidTemplateRendererUnitTests
    {
        private readonly Mock<ITranslationService> _translationServiceMock;
        private readonly Mock<IBlobUrlResolver> _blobUrlResolverMock;
        private readonly Mock<INotificationLayoutService> _notificationLayoutServiceMock;
        private readonly Mock<INotificationLayoutSearchService> _notificationLayoutSearchService;
        private readonly LiquidTemplateRenderer _liquidTemplateRenderer;

        public LiquidTemplateRendererUnitTests()
        {
            _translationServiceMock = new Mock<ITranslationService>();
            _blobUrlResolverMock = new Mock<IBlobUrlResolver>();
            _notificationLayoutServiceMock = new Mock<INotificationLayoutService>();

            _notificationLayoutSearchService = new Mock<INotificationLayoutSearchService>();
            var notificationLayoutSearchResult = new NotificationLayoutSearchResult() { Results = new List<NotificationLayout>() };
            _notificationLayoutSearchService.Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>())).ReturnsAsync(notificationLayoutSearchResult);

            Func<ITemplateLoader> factory = () => new LayoutTemplateLoader(_notificationLayoutServiceMock.Object);
            _liquidTemplateRenderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions() { CustomFilterTypes = new HashSet<Type> { typeof(UrlFilters), typeof(TranslationFilter), typeof(ArrayFilter) } }), factory, _notificationLayoutSearchService.Object);


            //TODO
            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
            {
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(_translationServiceMock.Object, _blobUrlResolverMock.Object));
            }
            else
            {
                AbstractTypeFactory<NotificationScriptObject>.OverrideType<NotificationScriptObject, NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(_translationServiceMock.Object, _blobUrlResolverMock.Object));
            }
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
                Model = new { CreatedDate = DateTime.UtcNow },
            };

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(context);

            //Assert
            Assert.Equal(DateTime.UtcNow.Year.ToString(), result);
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

        [Fact]
        public async Task RenderAsync_ContextHasLayoutId_LayoutRendered()
        {
            var layoutId = Guid.NewGuid().ToString();

            var layout = new NotificationLayout
            {
                Id = layoutId,
                Template = "header {{content}} {{% if currency == 'en-US'}} {{}}  footer",
            };

            _notificationLayoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(x => x.FirstOrDefault() == layoutId), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([layout]);

            var content = @"{% capture content %}test_content{% endcapture %}";
            var context = new NotificationRenderContext
            {
                Template = content,
                LayoutId = layoutId,
                Model = new { Currency = "en-US" },
            };

            var result = await _liquidTemplateRenderer.RenderAsync(context);
            Assert.Equal("header test_content footer", result);
        }

        [Fact]
        public async Task RenderAsync_ContextHasLayoutId_Conditions_LayoutRendered()
        {
            var layoutId = Guid.NewGuid().ToString();
            var layout = new NotificationLayout
            {
                Id = layoutId,
                Template = "header {{content}} {{ if currency == 'en-US' }}en-US template{{ else }}other template{{ end }} {{}}  footer",
            };
            _notificationLayoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(x => x.FirstOrDefault() == layoutId), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([layout]);

            var content = @"{% capture content %} {% if currency == 'en-US' %} en-US content {% else %} other content {% endif %} {% endcapture %}";
            var context = new NotificationRenderContext
            {
                Template = content,
                LayoutId = layoutId,
                UseLayouts = true,
                Model = new { Currency = "en-US" },
            };

            var result = await _liquidTemplateRenderer.RenderAsync(context);
            Assert.Equal("header   en-US content   en-US template   footer", result);
        }

        [Theory]
        [InlineData("Group", "==", "Group2", "Item2", "Item4")]
        [InlineData("Group", "!=", "Group22", "Item1", "Item4")]
        [InlineData("Value", ">", "2", "Item3", "Item5")]
        [InlineData("Value", ">=", "2", "Item2", "Item5")]
        [InlineData("Value", "<", "4", "Item1", "Item3")]
        [InlineData("Value", "<=", "4", "Item1", "Item4")]
        [InlineData("Group", "contains", "2", "Item2", "Item5")]
        public async Task RenderAsync_FilterArray(string propertyName, string operationName, string propertyValue, string expectedFirstName, string expectedLastName)
        {
            var filter = $"'{propertyName}' '{operationName}' '{propertyValue}'";

            var context = new NotificationRenderContext
            {
                Template = "{{ filtered = items | where: " + filter + " }}" +
                           "{{ first_item = filtered | array.first }}" +
                           "{{ last_item = filtered | array.last }}" +
                           "First: {{ first_item.name }}, Last: {{ last_item.name }}",
                Model = new
                {
                    Items = new[]
                    {
                        new { Group = "Group1", Name = "Item1", Value = 1 },
                        new { Group = "Group2", Name = "Item2", Value = 2 },
                        new { Group = "Group2", Name = "Item3", Value = 3 },
                        new { Group = "Group2", Name = "Item4", Value = 4 },
                        new { Group = "Group22", Name = "Item5", Value = 5 },
                    }
                },
            };

            var expectedResult = $"First: {expectedFirstName}, Last: {expectedLastName}";

            var result = await _liquidTemplateRenderer.RenderAsync(context);
            Assert.Equal(expectedResult, result);
        }

        public static IEnumerable<object[]> TranslateData
        {
            get
            {
                return
                [
                    ["en", JObject.FromObject(new { order = new { subject1 = "subj" } })],
                    [null, JObject.FromObject(new { order = new { subject1 = "subj" } })]
                ];
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
