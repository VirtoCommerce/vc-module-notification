using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model.Search
{
    public class NotificationMessageSearchCriteria : SearchCriteriaBase
    {
        public string NotificationType { get; set; }
        public string Status { get; set; }
        public bool SearchInBody { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
