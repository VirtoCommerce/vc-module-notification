using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LayoutTemplateLoader : ITemplateLoader
    {
        private readonly INotificationLayoutService _notificationLayoutService;
        private readonly INotificationLayoutRegistrar _layoutRegistrar;

        public LayoutTemplateLoader(INotificationLayoutService notificationLayoutService, INotificationLayoutRegistrar layoutRegistrar)
        {
            _notificationLayoutService = notificationLayoutService;
            _layoutRegistrar = layoutRegistrar;
        }

        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            return templateName;
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return GetLayoutTemplate(templatePath).GetAwaiter().GetResult();
        }

        public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            // use templatePath as notification layout ID or predefined layout name
            return await GetLayoutTemplate(templatePath);
        }

        private async Task<string> GetLayoutTemplate(string layoutId)
        {
            if (string.IsNullOrEmpty(layoutId))
            {
                return string.Empty;
            }

            var layout = await _notificationLayoutService.GetNoCloneAsync(layoutId);
            if (layout != null)
            {
                return layout.Template ?? string.Empty;
            }

            // Fallback: predefined layout from registrar (layoutId == layout name for predefined layouts)
            var predefined = _layoutRegistrar.GetByName(layoutId);
            return predefined?.Template ?? string.Empty;
        }
    }
}
