using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LayoutTemplateLoader : ITemplateLoader
    {
        private ICrudService<NotificationLayout> _notificationLayoutService;

        private readonly string _layoutName = "layout";

        private string _notificationLayoutId;

        public LayoutTemplateLoader(ICrudService<NotificationLayout> notificationLayoutService, string notificationLayoutId)
        {
            _notificationLayoutService = notificationLayoutService;
            _notificationLayoutId = notificationLayoutId;
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
            if (_notificationLayoutId == null || !templatePath.EqualsInvariant(_layoutName))
            {
                return string.Empty;
            }

            var layout = await _notificationLayoutService.GetByIdAsync(_notificationLayoutId);

            var result = layout?.Template ?? string.Empty;
            return result;
        }
    }
}
