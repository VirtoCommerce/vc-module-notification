using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// Rendering text by Templates 
    /// </summary>
    public interface INotificationTemplateRenderer
    {
        Task<string> RenderAsync(NotificationRenderContext renderContext);
    }
}
