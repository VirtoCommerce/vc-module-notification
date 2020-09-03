using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.Data.TemplateLoaders
{
    public static class NotificationBuilderExtensions
    {
        public static NotificationBuilder WithTemplatesFromPath(this NotificationBuilder builder, string path, string fallbackPath = null)
        {
            var templateLoader = builder.ServiceProvider.GetService<INotificationTemplateLoader>();
            var options = builder.ServiceProvider.GetService<IOptions<FileSystemTemplateLoaderOptions>>();

            if (builder.Notification.Templates == null)
            {
                builder.Notification.Templates = new List<NotificationTemplate>();
            }
            path = string.IsNullOrEmpty(path) ? options.Value.DiscoveryPath : path;
            fallbackPath = string.IsNullOrEmpty(fallbackPath) ? options.Value.FallbackDiscoveryPath : fallbackPath;
            if (!string.IsNullOrEmpty(path))
            {
                builder.WithTemplates(templateLoader.LoadTemplates(builder.Notification, path, fallbackPath).ToArray());
            }

            return builder;
        }
    }
}
