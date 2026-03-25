using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer;

public class LiquidTemplateRenderer : INotificationTemplateRenderer
{
    private readonly LiquidRenderOptions _options;
    private readonly Func<ITemplateLoader> _templateLoaderFactory;
    private readonly INotificationLayoutSearchService _notificationLayoutSearchService;
    private readonly INotificationLayoutRegistrar _layoutRegistrar;

    public LiquidTemplateRenderer(
        IOptions<LiquidRenderOptions> options,
        Func<ITemplateLoader> templateLoaderFactory,
        INotificationLayoutSearchService notificationLayoutSearchService,
        INotificationLayoutRegistrar layoutRegistrar)
    {
        _options = options.Value;
        _templateLoaderFactory = templateLoaderFactory;
        _notificationLayoutSearchService = notificationLayoutSearchService;
        _layoutRegistrar = layoutRegistrar;
    }

    public async Task<string> RenderAsync(NotificationRenderContext renderContext)
    {
        var templateContext = new LiquidTemplateContext()
        {
            EnableRelaxedMemberAccess = true,
            NewLine = Environment.NewLine,
            TemplateLoaderLexerOptions = new LexerOptions
            {
                Lang = _options.TemplateScriptLanguage,
                Mode = ScriptMode.Default,
            },
            LoopLimit = _options.LoopLimit,
        };

        var stringTemplate = renderContext.Template;

        if (renderContext.UseLayouts && string.IsNullOrEmpty(renderContext.LayoutId))
        {
            var layoutSearchResult = await _notificationLayoutSearchService.SearchAsync(new NotificationLayoutSearchCriteria() { IsDefault = true });
            renderContext.LayoutId = layoutSearchResult.Results.FirstOrDefault()?.Id
                ?? await GetPredefinedDefaultLayoutIdAsync();
        }

        if (!string.IsNullOrEmpty(renderContext.LayoutId))
        {
            stringTemplate = IncludeLayout(stringTemplate, renderContext.LayoutId);
            templateContext.TemplateLoader = _templateLoaderFactory();
        }

        var scriptObject = AbstractTypeFactory<NotificationScriptObject>.TryCreateInstance();
        scriptObject.Language = renderContext.Language;
        scriptObject.Import(renderContext.Model);
        foreach (var customFilterType in _options.CustomFilterTypes)
        {
            scriptObject.Import(customFilterType);
        }
        templateContext.PushGlobal(scriptObject);

        var template = Template.ParseLiquid(stringTemplate);
        var result = await template.RenderAsync(templateContext);

        return result;
    }

    /// <summary>
    /// Returns the name of the predefined default layout if it has no DB override, otherwise null.
    /// Respects the "DB takes precedence" rule: if an admin saved a DB override with IsDefault=false,
    /// the predefined default is not used.
    /// </summary>
    private async Task<string> GetPredefinedDefaultLayoutIdAsync()
    {
        var predefinedDefault = _layoutRegistrar.AllRegisteredLayouts.FirstOrDefault(x => x.IsDefault);
        if (predefinedDefault == null)
        {
            return null;
        }

        var hasDbOverride = (await _notificationLayoutSearchService.SearchAsync(
            new NotificationLayoutSearchCriteria { Names = [predefinedDefault.Name], Take = 1 }))
            .Results.Any();

        return hasDbOverride ? null : predefinedDefault.Name;
    }

    /// <summary>
    /// Append 'include' directive to the end of a template string for force layout loader
    /// </summary>
    private static string IncludeLayout(string template, string layoutId)
    {
        var layout = $"{{{{include '{layoutId}'}}}}";
        var stringBuilder = new StringBuilder(template);
        stringBuilder.Append(layout);
        return stringBuilder.ToString();
    }
}
