using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
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
            _liquidTemplateRenderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions() { CustomFilterTypes = new HashSet<Type> { typeof(UrlFilters), typeof(TranslationFilter), typeof(ArrayFilter), typeof(StandardFilters) } }), factory, _notificationLayoutSearchService.Object);

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
            var content = @"{% capture content %}test_content{% endcapture %}";
            var layout = new NotificationLayout
            {
                Id = layoutId,
                Template = "header {{content}} footer",
            };

            _notificationLayoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(x => x.FirstOrDefault() == layoutId), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { layout });

            var context = new NotificationRenderContext
            {
                Template = content,
                LayoutId = layoutId,
            };

            var result = await _liquidTemplateRenderer.RenderAsync(context);
            Assert.Equal("header test_content footer", result);
        }

        [Fact]
        public async Task RenderAsync_FilterArrayEqual_ContextHasLayoutId_ArrayContent_LayoutRendered()
        {
            var layoutId = Guid.NewGuid().ToString();
            var content = @"{% capture content %} Items Catalog Name: {{ var1 = customer_order.items | where: 'CatalogId' '==' '1' | array.first }} {{ var1.name }} {% endcapture %}";
            
            dynamic model = new
            {
                CustomerOrder = new CustomerOrder
                {
                    Number = "123456",
                    Total = 3,
                    Items = new[]
                    {
                        new LineItem{CatalogId = "1",Name = "Item1"},
                        new LineItem{CatalogId= "2",Name = "Item2"},
                        new LineItem{CatalogId = "1",Name = "Item3"},
                    }
                }                
            };
            
            var layout = new NotificationLayout
            {
                Id = layoutId,
                Template = "header {{content}} footer",
            };

            _notificationLayoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(x => x.FirstOrDefault() == layoutId), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { layout });

            var context = new NotificationRenderContext
            {
                Template = content,
                LayoutId = layoutId,
                Model = model
            };
            
            var result = await _liquidTemplateRenderer.RenderAsync(context);
            Assert.Equal("header  Items Catalog Name:  Item1  footer", result);
        }

        [Fact]
        public async Task RenderAsync_FilterArrayGreaterThan_ContextHasLayoutId_ArrayContent_LayoutRendered()
        {
            var layoutId = Guid.NewGuid().ToString();
            var content = @"{% capture content %} Items Catalog Name: {{ var1 = customer_order.items | where: 'DiscountAmount' '>' '5' | array.first }} {{ var1.name }} {% endcapture %}";

            dynamic model = new
            {
                CustomerOrder = new CustomerOrder
                {
                    Number = "123456",
                    Total = 3,
                    Items = new[]
                    {
                        new LineItem{CatalogId = "1",Name = "Item1", DiscountAmount = 5},
                        new LineItem{CatalogId= "2",Name = "Item2", DiscountAmount = 10 },
                        new LineItem{CatalogId = "1",Name = "Item3",DiscountAmount = 5},
                    }
                }
            };

            var layout = new NotificationLayout
            {
                Id = layoutId,
                Template = "header {{content}} footer",
            };

            _notificationLayoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(x => x.FirstOrDefault() == layoutId), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new[] { layout });

            var context = new NotificationRenderContext
            {
                Template = content,
                LayoutId = layoutId,
                Model = model
            };

            var result = await _liquidTemplateRenderer.RenderAsync(context);
            Assert.Equal("header  Items Catalog Name:  Item2  footer", result);
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
