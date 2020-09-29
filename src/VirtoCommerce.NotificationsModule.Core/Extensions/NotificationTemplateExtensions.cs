using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Extensions
{
    public static class NotificationTemplateExtensions
    {
        /// <summary>
        /// Finds the template for the given language in the collection of templates. Templates saved in database has priority over predefined ones.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="language"></param>
        /// <returns>The template for the language.</returns>
        public static T FindTemplateForLanguage<T>(this IEnumerable<T> items, string language) where T : NotificationTemplate
        {
            return items.Where(x => string.IsNullOrEmpty(x.LanguageCode) || x.LanguageCode.EqualsInvariant(language))
                .OrderByDescending(x => x.LanguageCode)
                .ThenBy(x => x.IsPredefined)
                .ThenByDescending(x => x.ModifiedDate)
                .FirstOrDefault();
        }
    }
}
