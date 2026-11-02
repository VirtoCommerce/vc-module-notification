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
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Localizations;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    /// <summary>
    /// Renders the shipped snippets (NotificationSnippets/*.html) alone and inside the Default Master.
    /// </summary>
    public class SnippetLibraryUnitTests
    {
        private static readonly object Order = new
        {
            CustomerOrder = new
            {
                Number = "CO-100345",
                Currency = "USD",
                SubTotal = 15400.0000m,
                DiscountTotal = 0m,
                ShippingTotal = 0m,
                TaxTotal = 50m,
                Total = 15450m,
                Items = new[]
                {
                    new { Name = "Jumbo GG Canvas Jacket", Sku = "WH-4471", Quantity = 1, ExtendedPrice = 2200m, ImageUrl = "https://cdn.example.com/jacket.jpg" },
                    new { Name = "Blue <Rumba> Jacket", Sku = "WH-4472", Quantity = 8, ExtendedPrice = 13200m, ImageUrl = (string)null },
                },
                Addresses = new[]
                {
                    new { FirstName = "Alex", LastName = "Smith", Organization = (string)null, Line1 = "20945 Devonshire St", Line2 = "Suite 102", City = "Los Angeles", RegionName = "California", PostalCode = "91311", CountryName = "United States", Phone = (string)null },
                },
            },
        };

        private const string AllSnippetsBody = "{% capture content %}"
            + "{% include 'snippets/eyebrow' eyebrow_text: 'Order placed' %}"
            + "{% include 'snippets/heading' heading_text: 'Thanks for your order!' %}"
            + "{% include 'snippets/text' text_body: 'We are getting it ready.' %}"
            + "{% include 'snippets/items' items_list: customer_order.items, items_currency: customer_order.currency %}"
            + "{% include 'snippets/totals' totals_source: customer_order %}"
            + "{% include 'snippets/button' button_url: 'https://shop/orders/1', button_label: 'View order' %}"
            + "{% include 'snippets/divider' %}"
            + "{% include 'snippets/spacer' spacer_size: 24 %}"
            + "{% include 'snippets/address' address_value: customer_order.addresses[0], address_title: 'Shipping address' %}"
            + "{% include 'snippets/tracking' tracking_number: '1Z-8843', tracking_carrier: 'DHL Express' %}"
            + "{% include 'snippets/notice' notice_text: 'Questions? Reply to this email.', notice_style: 'warning' %}"
            + "{% include 'snippets/image' image_url: 'https://cdn.example.com/banner.jpg', image_alt: 'Sale' %}"
            + "{% include 'snippets/benefits' benefits_items: 'Fast delivery|Easy payment|Order rewards' %}"
            + "{% endcapture %}";

        private readonly NotificationSnippetRegistrar _snippetRegistrar = new();
        private readonly LiquidTemplateRenderer _renderer;

        public SnippetLibraryUnitTests()
        {
            _snippetRegistrar.RegisterSnippetsFromPath(Path.Combine(AppContext.BaseDirectory, "NotificationSnippets"));

            var layoutTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "NotificationLayouts", "Default.html"));
            var layoutService = new Mock<INotificationLayoutService>();
            layoutService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<NotificationLayout> { new() { Id = ModuleConstants.DefaultLayoutName, Template = layoutTemplate } });

            var layoutSearchService = new Mock<INotificationLayoutSearchService>();
            layoutSearchService
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult { Results = new List<NotificationLayout>() });

            Func<ITemplateLoader> factory = () => new NotificationTemplateLoader(new LayoutTemplateLoader(layoutService.Object), _snippetRegistrar);
            _renderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions { CustomFilterTypes = [typeof(MoneyFilter)] }), factory, layoutSearchService.Object);

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
            {
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(Mock.Of<ITranslationService>(), Mock.Of<IBlobUrlResolver>()));
            }
        }

        [Fact]
        public void RegisterSnippetsFromPath_ShippedFolder_AllSnippetsRegistered()
        {
            Assert.Equal(
                ["address", "benefits", "button", "divider", "eyebrow", "heading", "image", "items", "notice", "spacer", "text", "totals", "tracking"],
                _snippetRegistrar.AllRegisteredSnippets.Keys.Order());
        }

        [Fact]
        public async Task RenderAsync_AllSnippetsInDefaultMaster_Rendered()
        {
            var result = await Render(AllSnippetsBody, ModuleConstants.DefaultLayoutName);

            Assert.Contains("Order placed</div>", result);
            Assert.Contains("Thanks for your order!</h1>", result);
            Assert.Contains("Jumbo GG Canvas Jacket", result);
            Assert.Contains("Blue &lt;Rumba&gt; Jacket", result);
            Assert.Contains("Qty: 8", result);
            Assert.Contains("$13,200.00", result);
            Assert.Contains("$15,450.00", result);
            Assert.Contains("<td style=\"padding:6px 2px;\">Tax</td>", result);
            Assert.DoesNotContain(">Discount<", result);
            Assert.Contains("href=\"https://shop/orders/1\"", result);
            Assert.Contains("fillcolor=\"#e8912a\"", result);
            Assert.Contains("Shipping address", result);
            Assert.Contains("Los Angeles, California 91311", result);
            Assert.Contains("DHL Express", result);
            Assert.Contains("#fdf3e4", result);
            Assert.Contains("alt=\"Sale\"", result);
            Assert.Contains("Order rewards</div>", result);
            Assert.DoesNotContain("eyebrow_text:", result);
            Assert.DoesNotContain("{{", result);
            Assert.DoesNotContain("{%", result);
        }

        [Fact]
        public async Task RenderAsync_AllSnippets_ArgumentsClearedAfterwards()
        {
            const string probe = "[{{ eyebrow_text }}{{ heading_text }}{{ text_body }}{{ items_list }}{{ items_currency }}{{ items_item }}"
                + "{{ totals_source }}{{ totals_currency }}{{ button_url }}{{ button_label }}{{ button_color }}{{ button_fill }}"
                + "{{ spacer_size }}{{ address_value }}{{ address_title }}{{ tracking_number }}{{ tracking_carrier }}"
                + "{{ notice_text }}{{ notice_style }}{{ notice_background }}{{ image_url }}{{ image_alt }}{{ benefits_items }}{{ benefits_list }}]";

            var result = await Render(AllSnippetsBody + probe);

            Assert.EndsWith("[]", result);
        }

        [Fact]
        public async Task RenderAsync_ButtonSecondary_Outlined()
        {
            var result = await Render("{% include 'snippets/button' button_url: 'u', button_label: 'Track', button_style: 'secondary' %}");

            Assert.Contains("fillcolor=\"#ffffff\"", result);
            Assert.Contains("color:#e8912a;", result);
        }

        [Fact]
        public async Task RenderAsync_TotalsWithDiscountAndShipping_RowsShown()
        {
            var result = await Render(
                "{% include 'snippets/totals' totals_source: cart %}",
                model: new { Cart = new { Currency = "EUR", SubTotal = 100m, DiscountTotal = 10m, ShippingTotal = 5m, TaxTotal = 0m, Total = 95m } },
                language: "de-DE");

            Assert.Contains("&minus;10,00 €", result);
            Assert.Contains(">Shipping<", result);
            Assert.DoesNotContain(">Tax<", result);
            Assert.Contains("95,00 €", result);
        }

        [Theory]
        [InlineData(1157.39, "USD", "en-US", "$1,157.39")]
        [InlineData(1157.39, "EUR", "de-DE", "1.157,39 €")]
        [InlineData(399.0000, "USD", null, "$399.00")]
        [InlineData(5, null, "en-US", "5.00")]
        [InlineData(5, "XYZ", "en-US", "XYZ5.00")]
        public async Task Money_FormatsInNotificationLanguage(double amount, string currency, string language, string expected)
        {
            var result = await Render("{{ amount | money: currency }}", model: new { Amount = (decimal)amount, Currency = currency }, language: language);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Money_Null_Empty()
        {
            var result = await Render("[{{ amount | money: 'USD' }}]", model: new { Amount = (decimal?)null });

            Assert.Equal("[]", result);
        }

        private Task<string> Render(string template, string layoutId = null, object model = null, string language = null)
        {
            return _renderer.RenderAsync(new NotificationRenderContext
            {
                Template = template,
                Model = model ?? Order,
                LayoutId = layoutId,
                Language = language,
            });
        }
    }
}
