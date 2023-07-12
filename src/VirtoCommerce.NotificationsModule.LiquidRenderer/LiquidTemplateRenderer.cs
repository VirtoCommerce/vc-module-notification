using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LiquidTemplateRenderer : INotificationTemplateRenderer
    {
        private readonly LiquidRenderOptions _options;
        private readonly Func<ITemplateLoader> _templateLoaderFactory;
        private readonly INotificationLayoutSearchService _notificationLayoutSearchService;

        public LiquidTemplateRenderer(IOptions<LiquidRenderOptions> options
            , Func<ITemplateLoader> templateLoaderFactory
            , INotificationLayoutSearchService notificationLayoutSearchService)
        {
            _options = options.Value;
            _templateLoaderFactory = templateLoaderFactory;
            _notificationLayoutSearchService = notificationLayoutSearchService;
        }

        public Task<string> RenderAsync(string stringTemplate, object model, string language = null)
        {
            var renderContext = new NotificationRenderContext
            {
                Template = stringTemplate,
                Language = language,
                Model = model,
            };

            return RenderAsync(renderContext);
        }

        public async Task<string> RenderAsync(NotificationRenderContext renderContext)
        {
            var templateContext = new LiquidTemplateContext()
            {
                EnableRelaxedMemberAccess = true,
                NewLine = Environment.NewLine,
                TemplateLoaderLexerOptions = new LexerOptions
                {
                    Mode = ScriptMode.Default,
                },
                LoopLimit = _options.LoopLimit,
            };

            var stringTemplate = renderContext.Template;

            if (renderContext.UseLayouts && string.IsNullOrEmpty(renderContext.LayoutId))
            {
                var layoutSearchResult = await _notificationLayoutSearchService.SearchAsync(new NotificationLayoutSearchCriteria() { IsDefault = true });
                renderContext.LayoutId = layoutSearchResult.Results.FirstOrDefault()?.Id;
            }

            if (!string.IsNullOrEmpty(renderContext.LayoutId))
            {
                stringTemplate = IncludeLayout(stringTemplate, renderContext.LayoutId);
                templateContext.TemplateLoader = _templateLoaderFactory();
            }

            var scriptObject = AbstractTypeFactory<NotificationScriptObject>.TryCreateInstance();
            scriptObject.Language = renderContext.Language;
            scriptObject.Import(renderContext.Model);
            foreach (var customFilterType in _options.CustomFilterTypes)
            {
                scriptObject.Import(customFilterType);
            }
            templateContext.PushGlobal(scriptObject);

            var template = Template.ParseLiquid(stringTemplate);
            var result = await template.RenderAsync(templateContext);

            return result;
        }

        /// <summary>
        /// Append 'include' directive to the end of a template string for force layout loader
        /// </summary>
        private static string IncludeLayout(string template, string layoutId)
        {
            var layout = $"{{{{include '{layoutId}'}}}}";
            var stringBuilder = new StringBuilder(template);
            stringBuilder.Append(layout);
            return stringBuilder.ToString();
        }
    }
}
