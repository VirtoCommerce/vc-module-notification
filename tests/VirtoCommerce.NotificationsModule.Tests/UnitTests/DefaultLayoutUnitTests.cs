using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Scriban.Runtime;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.NotificationsModule.Core;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Localizations;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    /// <summary>
    /// Renders the shipped Default Master (NotificationLayouts/Default.html) with the real Liquid renderer.
    /// </summary>
    public class DefaultLayoutUnitTests
    {
        private const string Body = "{% capture content %}<p>Hello</p>{% endcapture %}";

        private readonly LiquidTemplateRenderer _renderer;

        public DefaultLayoutUnitTests()
        {
            var layoutTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "NotificationLayouts", "Default.html"));
            var layoutService = new Mock<INotificationLayoutService>();
            layoutService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<NotificationLayout> { new() { Id = ModuleConstants.DefaultLayoutName, Template = layoutTemplate } });

            var layoutSearchService = new Mock<INotificationLayoutSearchService>();
            layoutSearchService
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult { Results = new List<NotificationLayout>() });

            Func<ITemplateLoader> factory = () => new NotificationTemplateLoader(new LayoutTemplateLoader(layoutService.Object), new NotificationSnippetRegistrar());
            _renderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions()), factory, layoutSearchService.Object);

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
            {
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(Mock.Of<ITranslationService>(), Mock.Of<IBlobUrlResolver>()));
            }
        }

        [Fact]
        public async Task RenderAsync_NoBrand_DefaultsUsed()
        {
            var result = await Render(new { });

            Assert.Contains("<p>Hello</p>", result);
            Assert.Contains("Virto Commerce", result);
            Assert.Contains("max-width:600px", result);
            Assert.Contains("max-width:568px", result);
            Assert.Contains("#e8912a", result);
            Assert.Contains($"&copy; {DateTime.UtcNow.Year} Virto Commerce", result);
            Assert.DoesNotContain("Unsubscribe", result);
            Assert.DoesNotContain("{{", result);
            Assert.DoesNotContain("{%", result);
        }

        [Fact]
        public async Task RenderAsync_BrandSet_BrandValuesUsed()
        {
            var result = await Render(new
            {
                Brand = new
                {
                    LogoText = "Acme",
                    LogoUrl = "https://cdn.example.com/logo.png",
                    AccentColor = "#3b7ddd",
                    HeaderStyle = "dark",
                    EmailWidth = 700,
                    FooterText = "Acme Inc.",
                    Address = "1 Main St\nSpringfield",
                    SocialLinks = new[] { new { Name = "LinkedIn", Url = "https://linkedin.com/acme" } },
                    UtilityLinks = new[] { new { Name = "Privacy", Url = "https://acme.com/privacy" } },
                },
            });

            Assert.Contains("max-width:700px", result);
            Assert.Contains("max-width:668px", result);
            Assert.Contains("background-color:#16233f", result);
            Assert.Contains("src=\"https://cdn.example.com/logo.png\"", result);
            Assert.Contains("alt=\"Acme\"", result);
            Assert.Contains("#3b7ddd", result);
            Assert.Contains("Acme Inc.", result);
            Assert.Contains("1 Main St<br>Springfield", result);
            Assert.Contains("href=\"https://linkedin.com/acme\"", result);
            Assert.Contains("href=\"https://acme.com/privacy\"", result);
            Assert.DoesNotContain("Virto Commerce", result);
        }

        [Fact]
        public async Task RenderAsync_AccentHeader_AccentBackground()
        {
            var result = await Render(new { Brand = new { HeaderStyle = "accent", AccentColor = "#5e9e73" } });

            Assert.Contains("background-color:#5e9e73", result);
        }

        [Theory]
        [InlineData(true, "https://shop/unsubscribe", true)]
        [InlineData(true, null, false)]
        [InlineData(false, "https://shop/unsubscribe", false)]
        public async Task RenderAsync_Unsubscribe_ShownOnlyWhenEnabledAndUrlSet(bool showUnsubscribe, string unsubscribeUrl, bool expected)
        {
            var result = await Render(new { Brand = new { ShowUnsubscribe = showUnsubscribe }, UnsubscribeUrl = unsubscribeUrl });

            Assert.Equal(expected, result.Contains("Unsubscribe"));
        }

        [Fact]
        public async Task RenderAsync_Preheader_RenderedHidden()
        {
            var result = await Render(new { }, "{% assign preheader = 'Your order is on its way' %}" + Body);

            Assert.Contains(">Your order is on its way</div>", result);
        }

        private Task<string> Render(object model, string template = Body)
        {
            return _renderer.RenderAsync(new NotificationRenderContext
            {
                Template = template,
                Model = model,
                LayoutId = ModuleConstants.DefaultLayoutName,
            });
        }
    }
}
