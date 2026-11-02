using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using Scriban;
using Scriban.Syntax;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;

public static class MoneyFilter
{
    private static readonly ConcurrentDictionary<string, string> _currencySymbols = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Formats an amount in the notification language with the currency symbol:
    /// {{ customer_order.total | money: customer_order.currency }} renders $1,157.39 for en-US.
    /// Falls back to the invariant format with the currency code when the culture is not available.
    /// </summary>
    public static string Money(TemplateContext context, object amount, string currency = null)
    {
        if (amount is null)
        {
            return string.Empty;
        }

        var value = Convert.ToDecimal(amount, CultureInfo.InvariantCulture);
        var language = context.GetValue(new ScriptVariableGlobal(nameof(NotificationScriptObject.Language)))?.ToString();
        var culture = GetCulture(language);

        if (culture is null)
        {
            var number = value.ToString("N2", CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(currency) ? number : $"{number} {currency}";
        }

        // Without a currency code print the number only, rather than the culture's own currency symbol
        if (string.IsNullOrEmpty(currency))
        {
            return value.ToString("N2", culture);
        }

        var format = (NumberFormatInfo)culture.NumberFormat.Clone();
        format.CurrencyDecimalDigits = 2;
        format.CurrencySymbol = GetCurrencySymbol(currency);

        return value.ToString("C", format);
    }

    private static CultureInfo GetCulture(string language)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(string.IsNullOrEmpty(language) ? "en-US" : language);
            // In globalization-invariant mode every culture comes back as the invariant one
            return culture.Equals(CultureInfo.InvariantCulture) ? null : culture;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static string GetCurrencySymbol(string currency)
    {
        return _currencySymbols.GetOrAdd(currency, code =>
        {
            var region = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(x => new RegionInfo(x.Name))
                .FirstOrDefault(x => x.ISOCurrencySymbol.Equals(code, StringComparison.OrdinalIgnoreCase));

            return region?.CurrencySymbol ?? code;
        });
    }
}
