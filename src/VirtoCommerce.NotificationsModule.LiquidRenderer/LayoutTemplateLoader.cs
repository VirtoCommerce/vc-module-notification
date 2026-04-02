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

        public LayoutTemplateLoader(INotificationLayoutService notificationLayoutService)
        {
            _notificationLayoutService = notificationLayoutService;
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
            return await GetLayoutTemplate(templatePath);
        }

        private async Task<string> GetLayoutTemplate(string layoutId)
        {
            if (string.IsNullOrEmpty(layoutId))
            {
                return string.Empty;
            }

            var layout = await _notificationLayoutService.GetNoCloneAsync(layoutId);
            return layout?.Template ?? string.Empty;
        }
    }
}
