using System;
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
                builder.Notification.Templates = [];
            }

            builder.WithTemplates(templateLoader.LoadTemplates(builder.Notification, path, fallbackPath).ToArray());

            return builder;
        }

        /// <summary>
        /// Loads templates from <paramref name="path"/>, optionally clearing any templates
        /// inherited from the platform's prior registration before adding other ones.
        /// Set <paramref name="clear"/> to <c>true</c> when calling
        /// <see cref="VirtoCommerce.NotificationsModule.Core.Services.INotificationRegistrar.Notification{TNotification}"/>
        /// for a type that the platform / an upstream module has already registered with its own
        /// <c>WithTemplatesFromPath</c> — otherwise the platform template wins the
        /// <c>FindTemplateForLanguage</c> tie-break and silently shadows the other body.
        /// </summary>
        public static NotificationBuilder WithTemplatesFromPath(this NotificationBuilder builder, string path, bool clear)
        {
            if (clear)
            {
                builder.Notification.Templates?.Clear();
            }

            return builder.WithTemplatesFromPath(path);
        }
    }
}
