using System.Collections.Generic;

namespace VirtoCommerce.NotificationsModule.Core.Model;

/// <summary>
/// Brand of a store's emails, available in templates and layouts as `brand`, for example {{ brand.accent_color }}.
/// </summary>
public class NotificationBrand
{
    public string LogoText { get; set; }
    public string LogoUrl { get; set; }
    public int? LogoWidth { get; set; }
    public string AccentColor { get; set; }

    /// <summary>
    /// line, dark or accent
    /// </summary>
    public string HeaderStyle { get; set; }

    public int? EmailWidth { get; set; }
    public string FooterText { get; set; }
    public string Address { get; set; }
    public IList<NotificationBrandLink> SocialLinks { get; set; } = [];
    public IList<NotificationBrandLink> UtilityLinks { get; set; } = [];
    public bool ShowUnsubscribe { get; set; }
}

public class NotificationBrandLink
{
    public string Name { get; set; }
    public string Url { get; set; }
}
