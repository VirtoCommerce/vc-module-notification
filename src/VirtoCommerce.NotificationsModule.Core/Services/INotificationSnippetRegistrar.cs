using System.Collections.Generic;

namespace VirtoCommerce.NotificationsModule.Core.Services;

/// <summary>
/// Keeps reusable email snippets in memory. Templates and layouts include a snippet by its name
/// with the <see cref="SnippetPathPrefix"/> prefix: {% include 'snippets/button' button_url: url %}
/// </summary>
public interface INotificationSnippetRegistrar
{
    const string SnippetPathPrefix = "snippets/";

    IReadOnlyDictionary<string, string> AllRegisteredSnippets { get; }

    /// <summary>
    /// Returns the snippet template, or null if no snippet is registered with this name.
    /// </summary>
    string GetSnippet(string name);

    /// <summary>
    /// Registers a snippet or replaces the one registered with the same name.
    /// </summary>
    void RegisterSnippet(string name, string template);

    /// <summary>
    /// Registers each *.html file in the folder as a snippet named after the file without extension.
    /// </summary>
    void RegisterSnippetsFromPath(string path);
}
