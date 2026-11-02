using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Scriban.Runtime;
using Scriban.Syntax;
using VirtoCommerce.AssetsModule.Core.Assets;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Localizations;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    public class NotificationSnippetUnitTests
    {
        private const string ButtonSnippet = "<a href=\"{{ button_url }}\" class=\"{{ button_style | default: 'primary' }}\">{{ button_label }}</a>"
            + "{% assign button_url = nil %}{% assign button_label = nil %}{% assign button_style = nil %}";

        private readonly Mock<INotificationLayoutService> _notificationLayoutServiceMock = new();
        private readonly NotificationSnippetRegistrar _snippetRegistrar = new();
        private readonly LiquidTemplateRenderer _renderer;

        public NotificationSnippetUnitTests()
        {
            var notificationLayoutSearchService = new Mock<INotificationLayoutSearchService>();
            notificationLayoutSearchService
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult { Results = new List<NotificationLayout>() });

            Func<ITemplateLoader> factory = () => new NotificationTemplateLoader(
                new LayoutTemplateLoader(_notificationLayoutServiceMock.Object),
                _snippetRegistrar);
            _renderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions()), factory, notificationLayoutSearchService.Object);

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
            {
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(Mock.Of<ITranslationService>(), Mock.Of<IBlobUrlResolver>()));
            }

            _snippetRegistrar.RegisterSnippet("button", ButtonSnippet);
        }

        [Fact]
        public async Task RenderAsync_SnippetInclude_SnippetRendered()
        {
            var context = new NotificationRenderContext
            {
                Template = "{% include 'snippets/button' button_url: url, button_label: 'Reset' %}",
                Model = new { Url = "https://example.com/reset" },
            };

            var result = await _renderer.RenderAsync(context);

            Assert.Equal("<a href=\"https://example.com/reset\" class=\"primary\">Reset</a>", result);
        }

        [Fact]
        public async Task RenderAsync_LayoutIdAndSnippet_BothResolved()
        {
            SetupLayout("<body>{{ content }}</body>");
            var context = new NotificationRenderContext
            {
                Template = "{% capture content %}{% include 'snippets/button' button_url: url, button_label: 'Reset' %}{% endcapture %}",
                Model = new { Url = "https://example.com/reset" },
                LayoutId = "Default",
            };

            var result = await _renderer.RenderAsync(context);

            Assert.Equal("<body><a href=\"https://example.com/reset\" class=\"primary\">Reset</a></body>", result);
        }

        [Fact]
        public async Task RenderAsync_LayoutIncludesSnippet_SnippetRendered()
        {
            _snippetRegistrar.RegisterSnippet("divider", "<hr>");
            SetupLayout("{{ content }}{% include 'snippets/divider' %}");
            var context = new NotificationRenderContext
            {
                Template = "{% capture content %}Hi{% endcapture %}",
                LayoutId = "Default",
            };

            var result = await _renderer.RenderAsync(context);

            Assert.Equal("Hi<hr>", result);
        }

        [Fact]
        public async Task RenderAsync_SnippetCalledTwice_OptionalArgumentDoesNotCarryOver()
        {
            var context = new NotificationRenderContext
            {
                Template = "{% include 'snippets/button' button_url: url, button_label: 'A', button_style: 'secondary' %}"
                    + "{% include 'snippets/button' button_url: url, button_label: 'B' %}"
                    + "[{{ button_label }}][{{ url }}]",
                Model = new { Url = "u" },
            };

            var result = await _renderer.RenderAsync(context);

            Assert.Equal("<a href=\"u\" class=\"secondary\">A</a><a href=\"u\" class=\"primary\">B</a>[][u]", result);
        }

        [Fact]
        public async Task RenderAsync_UnknownSnippet_Throws()
        {
            var context = new NotificationRenderContext
            {
                Template = "{% include 'snippets/missing' %}",
            };

            var exception = await Assert.ThrowsAsync<ScriptRuntimeException>(() => _renderer.RenderAsync(context));

            Assert.Contains("snippets/missing", exception.Message);
        }

        [Fact]
        public void RegisterSnippetsFromPath_HtmlFiles_RegisteredByFileName()
        {
            var path = Directory.CreateTempSubdirectory().FullName;
            try
            {
                File.WriteAllText(Path.Combine(path, "button.html"), "<a></a>");
                File.WriteAllText(Path.Combine(path, "readme.md"), "not a snippet");

                var registrar = new NotificationSnippetRegistrar();
                registrar.RegisterSnippetsFromPath(path);

                Assert.Equal("<a></a>", registrar.GetSnippet("Button"));
                Assert.Single(registrar.AllRegisteredSnippets);
            }
            finally
            {
                Directory.Delete(path, recursive: true);
            }
        }

        [Fact]
        public void RegisterSnippetsFromPath_MissingFolder_NothingRegistered()
        {
            var registrar = new NotificationSnippetRegistrar();

            registrar.RegisterSnippetsFromPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            Assert.Empty(registrar.AllRegisteredSnippets);
        }
        private void SetupLayout(string template)
        {
            _notificationLayoutServiceMock
                .Setup(x => x.GetAsync(It.Is<IList<string>>(ids => ids.Contains("Default")), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<NotificationLayout> { new() { Id = "Default", Template = template } });
        }
    }
}
