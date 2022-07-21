using System;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// Rendering text by Templates 
    /// </summary>
    public interface INotificationTemplateRenderer
    {
        [Obsolete("Use RenderAsync(NotificationRenderContext renderContext) instead.")]
        Task<string> RenderAsync(string stringTemplate, object model, string language = null);

        Task<string> RenderAsync(NotificationRenderContext renderContext);
    }
}
