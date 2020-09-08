using System;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFileSystemTemplateLoader(this IServiceCollection serviceCollection, Action<FileSystemTemplateLoaderOptions> setupAction = null)
        {
            serviceCollection.AddTransient<INotificationTemplateLoader, FileSystemNotificationTemplateLoader>();
            serviceCollection.AddTransient<FileSystemNotificationTemplateLoader>();

            if (setupAction != null)
            {
                serviceCollection.Configure(setupAction);
            }
        }
    }
}
