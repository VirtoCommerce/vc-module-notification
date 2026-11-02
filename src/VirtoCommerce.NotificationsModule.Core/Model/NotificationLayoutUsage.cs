using System.Collections.Generic;

namespace VirtoCommerce.NotificationsModule.Core.Model;

/// <summary>
/// Notification types whose email templates are rendered inside a layout.
/// </summary>
public class NotificationLayoutUsage
{
    public string LayoutId { get; set; }

    public IList<string> NotificationTypes { get; set; } = [];

    public int Count => NotificationTypes.Count;
}
