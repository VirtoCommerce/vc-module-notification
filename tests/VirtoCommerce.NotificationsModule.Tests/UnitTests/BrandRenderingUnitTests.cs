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
using VirtoCommerce.NotificationsModule.Tests.NotificationTypes;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Localizations;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    /// <summary>
    /// The renderer adds the brand of the notification's store as `brand`, which the Default Master reads.
    /// </summary>
    public class BrandRenderingUnitTests
    {
        private const string Body = "{% capture content %}<p>Hello</p>{% endcapture %}";

        private readonly Mock<INotificationBrandService> _brandService = new();
        private readonly LiquidTemplateRenderer _renderer;

        public BrandRenderingUnitTests()
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
            _renderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions()), factory, layoutSearchService.Object, _brandService.Object);

            _brandService.Setup(x => x.GetBrandAsync(null)).ReturnsAsync(new NotificationBrand { LogoText = "Global brand", AccentColor = "#111111" });
            _brandService.Setup(x => x.GetBrandAsync("store-1")).ReturnsAsync(new NotificationBrand { LogoText = "Store brand", HeaderStyle = "dark", EmailWidth = 680 });

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
            {
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(Mock.Of<ITranslationService>(), Mock.Of<IBlobUrlResolver>()));
            }
        }

        [Fact]
        public async Task RenderAsync_StoreNotification_StoreBrandUsed()
        {
            var notification = new SampleEmailNotification { TenantIdentity = new TenantIdentity("store-1", "Store") };

            var result = await Render(notification);

            Assert.Contains("Store brand", result);
            Assert.Contains("background-color:#16233f", result);
            Assert.Contains("max-width:680px", result);
        }

        [Fact]
        public async Task RenderAsync_NotificationWithoutStore_GlobalBrandUsed()
        {
            var result = await Render(new SampleEmailNotification());

            Assert.Contains("Global brand", result);
            Assert.Contains("#111111", result);
        }

        [Fact]
        public async Task RenderAsync_ModelHasBrand_ModelBrandKept()
        {
            var result = await Render(new { Brand = new { LogoText = "Sample brand" } });

            Assert.Contains("Sample brand", result);
            _brandService.Verify(x => x.GetBrandAsync(It.IsAny<string>()), Times.Never);
        }

        private Task<string> Render(object model)
        {
            return _renderer.RenderAsync(new NotificationRenderContext
            {
                Template = Body,
                Model = model,
                LayoutId = ModuleConstants.DefaultLayoutName,
            });
        }
    }
}
