using System;
using Microsoft.Extensions.DependencyInjection;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LiquidRendererBuilder
    {
        public LiquidRendererBuilder(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            Services = services;
        }

        public IServiceCollection Services { get; }

        public LiquidRendererBuilder Configure(Action<LiquidRenderOptions> configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Services.Configure(configuration);

            return this;
        }

        public LiquidRendererBuilder AddCustomLiquidFilterType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return Configure(options => options.CustomFilterTypes.Add(type));
        }


    }
}
