using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public class NotificationLayout : AuditableEntity, ICloneable
    {
        public string Name { get; set; }

        public string Template { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
