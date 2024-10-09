using System;
using Microsoft.Extensions.DependencyInjection;
using Scriban.Parsing;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer;

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

    public LiquidRendererBuilder SetRendererLoopLimit(int loopLimit)
    {
        if (loopLimit <= 0)
        {
            throw new ArgumentException("Loop limit must be greater than zero.", nameof(loopLimit));
        }

        return Configure(options => options.LoopLimit = loopLimit);
    }

    public LiquidRendererBuilder SetTemplateLanguage(string templateLanguage)
    {
        if (Enum.TryParse(templateLanguage, out ScriptLang scriptLang))
        {
            return Configure(options => options.TemplateScriptLanguage = scriptLang);
        }

        return this;
    }
}
