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
        private readonly Func<string, ITemplateLoader> _templateLoaderFactory;

        private const string LayoutKeyword = "{{include 'layout'}}";

        public LiquidTemplateRenderer(IOptions<LiquidRenderOptions> options, Func<string, ITemplateLoader> templateLoaderFactory)
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
                stringTemplate = IncludeLayout(stringTemplate);

                context.TemplateLoader = _templateLoaderFactory(hasLayout.NotificationLayoutId);
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
        private string IncludeLayout(string template)
        {
            var stringBuilder = new StringBuilder(template);
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(LayoutKeyword);
            return stringBuilder.ToString();
        }
    }
}
