using System;
using System.Threading.Tasks;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer;

/// <summary>
/// Resolves 'snippets/*' includes from the snippet registrar and passes any other name to the layout loader.
/// </summary>
public class NotificationTemplateLoader : ITemplateLoader
{
    private readonly ITemplateLoader _layoutTemplateLoader;
    private readonly INotificationSnippetRegistrar _snippetRegistrar;

    public NotificationTemplateLoader(ITemplateLoader layoutTemplateLoader, INotificationSnippetRegistrar snippetRegistrar)
    {
        _layoutTemplateLoader = layoutTemplateLoader;
        _snippetRegistrar = snippetRegistrar;
    }

    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName)
    {
        return IsSnippet(templateName)
            ? templateName
            : _layoutTemplateLoader.GetPath(context, callerSpan, templateName);
    }

    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return IsSnippet(templatePath)
            ? GetSnippet(templatePath)
            : _layoutTemplateLoader.Load(context, callerSpan, templatePath);
    }

    public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
    {
        return IsSnippet(templatePath)
            ? ValueTask.FromResult(GetSnippet(templatePath))
            : _layoutTemplateLoader.LoadAsync(context, callerSpan, templatePath);
    }

    private static bool IsSnippet(string templatePath)
    {
        return templatePath?.StartsWith(INotificationSnippetRegistrar.SnippetPathPrefix, StringComparison.OrdinalIgnoreCase) == true;
    }

    // Null makes Scriban fail the include with the snippet name in the error, so a typo shows up in the preview
    private string GetSnippet(string templatePath)
    {
        return _snippetRegistrar.GetSnippet(templatePath[INotificationSnippetRegistrar.SnippetPathPrefix.Length..]);
    }
}
