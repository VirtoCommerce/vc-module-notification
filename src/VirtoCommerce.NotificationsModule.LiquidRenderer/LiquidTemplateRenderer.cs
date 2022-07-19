using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LiquidTemplateRenderer : INotificationTemplateRenderer
    {
        private readonly LiquidRenderOptions _options;
        private readonly Func<ITemplateLoader> _templateLoaderFactory;

        public LiquidTemplateRenderer(IOptions<LiquidRenderOptions> options, Func<ITemplateLoader> templateLoaderFactory)
        {
            _options = options.Value;
            _templateLoaderFactory = templateLoaderFactory;
        }

        public async Task<string> RenderAsync(string stringTemplate, object model, string language = null)
        {
            var context = new LiquidTemplateContext()
            {
                EnableRelaxedMemberAccess = true,
                NewLine = Environment.NewLine,
                TemplateLoaderLexerOptions = new LexerOptions
                {
                    Mode = ScriptMode.Default,
                    Lang = ScriptLang.Liquid,
                },
                LoopLimit = _options.LoopLimit,
            };

            if (model is IHasNotificationLayoutId hasLayout && !string.IsNullOrEmpty(hasLayout.NotificationLayoutId))
            {
                stringTemplate = IncludeLayout(stringTemplate, hasLayout.NotificationLayoutId);
                context.TemplateLoader = _templateLoaderFactory();
            }

            var scriptObject = AbstractTypeFactory<NotificationScriptObject>.TryCreateInstance();
            scriptObject.Language = language;
            scriptObject.Import(model);
            foreach (var customFilterType in _options.CustomFilterTypes)
            {
                scriptObject.Import(customFilterType);
            }
            context.PushGlobal(scriptObject);

            var template = Template.ParseLiquid(stringTemplate);
            var result = await template.RenderAsync(context);

            return result;
        }

        /// <summary>
        /// Append 'include' directive to the end of a template string for force layout loader
        /// </summary>
        private string IncludeLayout(string template, string layoutId)
        {
            var layout = $"{{{{include '{layoutId}'}}}}";
            var stringBuilder = new StringBuilder(template);
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(layout);
            return stringBuilder.ToString();
        }
    }
}
