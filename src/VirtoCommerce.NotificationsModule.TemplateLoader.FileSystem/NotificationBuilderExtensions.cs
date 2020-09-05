using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem
{
    public static class NotificationBuilderExtensions
    {
        public static NotificationBuilder WithTemplatesFromPath(this NotificationBuilder builder, string path, string fallbackPath = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            var templateLoader = builder.ServiceProvider.GetService<FileSystemNotificationTemplateLoader>();
            if (builder.Notification.Templates == null)
            {
                builder.Notification.Templates = new List<NotificationTemplate>();
            }

            builder.WithTemplates(templateLoader.LoadTemplates(builder.Notification, path, fallbackPath).ToArray());

            return builder;
        }
    }
}
