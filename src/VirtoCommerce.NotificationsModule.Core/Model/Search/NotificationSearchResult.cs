using System;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public class NotificationSearchResult : GenericSearchResult<Notification>, ICloneable
    {
        public object Clone()
        {
            var result = MemberwiseClone() as NotificationSearchResult;

            result.Results = Results?.Select(x => x.Clone()).OfType<Notification>().ToList();

            return result;
        }
    }
}
