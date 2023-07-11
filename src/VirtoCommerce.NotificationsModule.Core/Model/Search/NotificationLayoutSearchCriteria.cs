using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model.Search
{
    public class NotificationLayoutSearchCriteria : SearchCriteriaBase
    {
        public IList<string> Names { get; set; }

        public bool IsDefault { get; set; }
    }
}
