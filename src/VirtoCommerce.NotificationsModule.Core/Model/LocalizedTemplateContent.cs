using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public class LocalizedTemplateContent : ValueObject, IHasLanguageCode
    {
        public string LanguageCode { get; set; }
        public string Content { get; set; }
        public string PartName { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return LanguageCode;
            yield return PartName;
        }
    }
}
