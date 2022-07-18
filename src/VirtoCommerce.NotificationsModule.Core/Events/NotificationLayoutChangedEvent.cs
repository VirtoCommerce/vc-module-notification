using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.NotificationsModule.Core.Events
{
    public class NotificationLayoutChangedEvent : GenericChangedEntryEvent<NotificationLayout>
    {
        public NotificationLayoutChangedEvent(IEnumerable<GenericChangedEntry<NotificationLayout>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
