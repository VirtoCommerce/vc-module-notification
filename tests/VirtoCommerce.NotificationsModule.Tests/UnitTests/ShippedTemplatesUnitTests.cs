using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
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
    /// Every email template shipped in NotificationTemplates renders its own sample inside the Default Master.
    /// </summary>
    public class ShippedTemplatesUnitTests
    {
        // Gmail clips messages larger than this
        private const int MaxEmailBytes = 102 * 1024;

        private static readonly string TemplatesPath = Path.Combine(AppContext.BaseDirectory, "ShippedTemplates");

        private readonly LiquidTemplateRenderer _renderer;

        public ShippedTemplatesUnitTests()
        {
            var snippetRegistrar = new NotificationSnippetRegistrar();
            snippetRegistrar.RegisterSnippetsFromPath(Path.Combine(AppContext.BaseDirectory, "NotificationSnippets"));

            var layoutTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "NotificationLayouts", "Default.html"));
            var layoutService = new Mock<INotificationLayoutService>();
            layoutService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<NotificationLayout> { new() { Id = ModuleConstants.DefaultLayoutName, Template = layoutTemplate } });

            var layoutSearchService = new Mock<INotificationLayoutSearchService>();
            layoutSearchService
                .Setup(x => x.SearchAsync(It.IsAny<NotificationLayoutSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new NotificationLayoutSearchResult { Results = new List<NotificationLayout>() });

            Func<ITemplateLoader> factory = () => new NotificationTemplateLoader(new LayoutTemplateLoader(layoutService.Object), snippetRegistrar);
            _renderer = new LiquidTemplateRenderer(Options.Create(new LiquidRenderOptions { CustomFilterTypes = [typeof(MoneyFilter)] }), factory, layoutSearchService.Object);

            if (!AbstractTypeFactory<NotificationScriptObject>.AllTypeInfos.SelectMany(x => x.AllSubclasses).Contains(typeof(NotificationScriptObject)))
            {
                AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                    .WithFactory(() => new NotificationScriptObject(Mock.Of<ITranslationService>(), Mock.Of<IBlobUrlResolver>()));
            }
        }

        public static TheoryData<string> NotificationTypes()
        {
            var data = new TheoryData<string>();
            foreach (var file in Directory.EnumerateFiles(TemplatesPath, "*_body.html").Order())
            {
                data.Add(Path.GetFileName(file)[..^"_body.html".Length]);
            }
            return data;
        }

        [Theory]
        [MemberData(nameof(NotificationTypes))]
        public async Task ShippedTemplate_RendersSampleInDefaultMaster(string notificationType)
        {
            var body = File.ReadAllText(Path.Combine(TemplatesPath, $"{notificationType}_body.html"));
            var subject = File.ReadAllText(Path.Combine(TemplatesPath, $"{notificationType}_subject.txt"));
            var sample = ToScriptObject(JObject.Parse(File.ReadAllText(Path.Combine(TemplatesPath, $"{notificationType}_sample.json"))));

            var renderedSubject = await _renderer.RenderAsync(new NotificationRenderContext { Template = subject, Model = sample });
            var renderedBody = await _renderer.RenderAsync(new NotificationRenderContext { Template = body, Model = sample, LayoutId = ModuleConstants.DefaultLayoutName });

            Assert.False(string.IsNullOrWhiteSpace(renderedSubject));
            Assert.DoesNotContain("{{", renderedSubject);
            Assert.Contains("<h1", renderedBody);
            Assert.DoesNotContain("{{", renderedBody);
            Assert.DoesNotContain("{%", renderedBody);
            Assert.True(Encoding.UTF8.GetByteCount(renderedBody) < MaxEmailBytes, $"{notificationType} is {Encoding.UTF8.GetByteCount(renderedBody)} bytes");
        }

        // Sample JSON uses the property names of the notification (camelCase); templates use snake_case
        private static object ToScriptObject(JToken token) => token switch
        {
            JObject jObject => jObject.Properties().Aggregate(new ScriptObject(), (result, property) =>
            {
                result[StandardMemberRenamer.Rename(property.Name)] = ToScriptObject(property.Value);
                return result;
            }),
            JArray jArray => new ScriptArray(jArray.Select(ToScriptObject)),
            JValue jValue => jValue.Value,
            _ => null,
        };
    }
}
