using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Caching;

namespace VirtoCommerce.NotificationsModule.Data.Caching
{
    public class NotificationCacheRegion : CancellableCacheRegion<NotificationCacheRegion>
    {
        public static IChangeToken CreateChangeToken(string[] ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var changeTokens = new List<IChangeToken>() { CreateChangeToken() };
            foreach (var id in ids)
            {
                changeTokens.Add(CreateChangeTokenForKey(id));
            }
            return new CompositeChangeToken(changeTokens);
        }

        public static void ExpireEntity(Notification entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            ExpireTokenForKey(entity.Id);
        }
    }
}
