using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Localizations;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class LiquidTemplateRendererUnitTests
    {
        private readonly Mock<ITranslationService> _translationServiceMock;
        private readonly Mock<IBlobUrlResolver> _blobUrlResolverMock;
        private readonly LiquidTemplateRenderer _liquidTemplateRenderer;
        public LiquidTemplateRendererUnitTests()
        {
            _translationServiceMock = new Mock<ITranslationService>();
            _blobUrlResolverMock = new Mock<IBlobUrlResolver>();
            _liquidTemplateRenderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions() { CustomFilterTypes = new HashSet<Type> { typeof(UrlFilters), typeof(TranslationFilter) } }));
            
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
            string input = "{{ 'order.subject1' | translate: \"" + language + "\" }} test";

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(input, null);

            //Assert
            Assert.Equal("subj test", result);
        }

        [Fact]
        public async Task Render_GetCurrentYear()
        {
            //Arrange
            string input = "{{created_date | date.to_string '%Y' }}";

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(input, new { CreatedDate = DateTime.Now });

            //Assert
            Assert.Equal(DateTime.Now.Year.ToString(), result);
        }

        [Fact]
        public async Task Render_GetAssetUrl()
        {
            //Arrange
            _blobUrlResolverMock.Setup(x => x.GetAbsoluteUrl(It.IsAny<string>())).Returns("http://localhost:10645/assets/1.svg");
            string input = "test {{ '1.svg' | asset_url }}";

            //Act
            var result = await _liquidTemplateRenderer.RenderAsync(input, null);

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
}
