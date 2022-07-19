using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LayoutTemplateLoader : ITemplateLoader
    {
        private ICrudService<NotificationLayout> _notificationLayoutService;

        public LayoutTemplateLoader(ICrudService<NotificationLayout> notificationLayoutService)
        {
            _notificationLayoutService = notificationLayoutService;
        }

        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
        {
            return templateName;
        }

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            return LoadAsync(context, callerSpan, templatePath).GetAwaiter().GetResult();
        }

        public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                return string.Empty;
            }

            // use templatePath as notification layout ID
            var layout = await _notificationLayoutService.GetByIdAsync(templatePath);

            var result = layout?.Template ?? string.Empty;
            return result;
        }
    }
}
