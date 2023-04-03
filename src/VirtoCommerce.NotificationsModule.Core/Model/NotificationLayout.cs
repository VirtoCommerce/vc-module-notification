using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public class NotificationLayout : AuditableEntity, ICloneable
    {
        public string Name { get; set; }

        public string Template { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is NotificationLayout notificationLayout) return Name == notificationLayout.Name;
            return false;
        }

        public override int GetHashCode() => Name.GetHashCode();

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
