using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer;

public class NotificationSnippetRegistrar : INotificationSnippetRegistrar
{
    private readonly ConcurrentDictionary<string, string> _snippets = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> AllRegisteredSnippets => _snippets;

    public string GetSnippet(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return _snippets.GetValueOrDefault(name);
    }

    public void RegisterSnippet(string name, string template)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        _snippets[name] = template ?? string.Empty;
    }

    public void RegisterSnippetsFromPath(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(path, "*.html", SearchOption.TopDirectoryOnly))
        {
            RegisterSnippet(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));
        }
    }
}
