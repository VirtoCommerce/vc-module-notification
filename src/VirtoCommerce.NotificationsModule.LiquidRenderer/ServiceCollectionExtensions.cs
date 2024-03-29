using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public static class ServiceCollectionExtensions
    {
        public static LiquidRendererBuilder AddLiquidRenderer(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.TryAddTransient<Func<ITemplateLoader>>(provider => () => new LayoutTemplateLoader(provider.GetRequiredService<INotificationLayoutService>()));
            services.TryAddTransient<INotificationTemplateRenderer, LiquidTemplateRenderer>();

            return new LiquidRendererBuilder(services);
        }

        public static IServiceCollection AddLiquidRenderer(this IServiceCollection services, Action<LiquidRendererBuilder> configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration(services.AddLiquidRenderer());

            return services;
        }
    }
}
