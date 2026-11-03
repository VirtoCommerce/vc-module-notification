using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Settings;
using BrandSettings = VirtoCommerce.NotificationsModule.Core.ModuleConstants.Settings.Brand;

namespace VirtoCommerce.NotificationsModule.Data.Services;

public class NotificationBrandService : INotificationBrandService
{
    public const string StoreObjectType = "Store";

    private readonly ISettingsManager _settingsManager;

    public NotificationBrandService(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    public virtual async Task<NotificationBrand> GetBrandAsync(string storeId)
    {
        var names = BrandSettings.AllSettings.Select(x => x.Name).ToList();

        var globalSettings = (await _settingsManager.GetObjectSettingsAsync(names)).ToDictionary(x => x.Name);
        var storeSettings = string.IsNullOrEmpty(storeId)
            ? new Dictionary<string, ObjectSettingEntry>()
            : (await _settingsManager.GetObjectSettingsAsync(names, StoreObjectType, storeId)).ToDictionary(x => x.Name);

        // A value set on the store wins, then the global value, then the default of the setting
        object GetValue(SettingDescriptor descriptor)
        {
            var storeSetting = storeSettings.GetValueOrDefault(descriptor.Name);
            var globalSetting = globalSettings.GetValueOrDefault(descriptor.Name);
            return IsSet(storeSetting?.Value) ? storeSetting.Value
                : IsSet(globalSetting?.Value) ? globalSetting.Value
                : globalSetting?.DefaultValue ?? descriptor.DefaultValue;
        }

        return new NotificationBrand
        {
            LogoText = GetValue(BrandSettings.LogoText)?.ToString(),
            LogoUrl = GetValue(BrandSettings.LogoUrl)?.ToString(),
            LogoWidth = ToInt(GetValue(BrandSettings.LogoWidth)),
            AccentColor = GetValue(BrandSettings.AccentColor)?.ToString(),
            HeaderStyle = GetValue(BrandSettings.HeaderStyle)?.ToString(),
            EmailWidth = ToInt(GetValue(BrandSettings.EmailWidth)),
            FooterText = GetValue(BrandSettings.FooterText)?.ToString(),
            Address = GetValue(BrandSettings.Address)?.ToString(),
            SocialLinks = ToLinks(GetValue(BrandSettings.SocialLinks)),
            UtilityLinks = ToLinks(GetValue(BrandSettings.UtilityLinks)),
            ShowUnsubscribe = Convert.ToBoolean(GetValue(BrandSettings.ShowUnsubscribe) ?? false),
        };
    }

    private static bool IsSet(object value)
    {
        return value is not null && (value is not string text || !string.IsNullOrWhiteSpace(text));
    }

    private static int? ToInt(object value)
    {
        return value is null ? null : Convert.ToInt32(value);
    }

    /// <summary>
    /// Links are stored as JSON: [{ "name": "LinkedIn", "url": "https://..." }]. Invalid JSON gives no links.
    /// </summary>
    private static IList<NotificationBrandLink> ToLinks(object value)
    {
        if (value is not string json || string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonConvert.DeserializeObject<List<NotificationBrandLink>>(json)?
                .Where(x => !string.IsNullOrEmpty(x?.Url))
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
