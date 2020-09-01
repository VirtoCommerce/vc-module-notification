using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LiquidTemplateRenderer : INotificationTemplateRenderer
    {
        private readonly LiquidRenderOptions _options;
        public LiquidTemplateRenderer(IOptions<LiquidRenderOptions> options)
        {
            _options = options.Value;
        }
        public async Task<string> RenderAsync(string stringTemplate, object model, string language = null)
        {
            var context = new LiquidTemplateContext()
            {
                EnableRelaxedMemberAccess = true,
                NewLine = Environment.NewLine,
                TemplateLoaderLexerOptions = new LexerOptions
                {
                    Mode = ScriptMode.Liquid
                }
            };
            var scriptObject = AbstractTypeFactory<NotificationScriptObject>.TryCreateInstance();
            scriptObject.Language = language;
            scriptObject.Import(model);
            foreach(var customFilterType in _options.CustomFilterTypes)
            {
                scriptObject.Import(customFilterType);
            }
            context.PushGlobal(scriptObject);

            var template = Template.ParseLiquid(stringTemplate);
            var result = await template.RenderAsync(context);

            return result;
        }      
    }
}
