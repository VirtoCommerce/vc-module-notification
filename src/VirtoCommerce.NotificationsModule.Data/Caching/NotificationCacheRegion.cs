using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Caching;

namespace VirtoCommerce.NotificationsModule.Data.Caching
{
    public class NotificationCacheRegion : CancellableCacheRegion<NotificationCacheRegion>
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _entityRegionTokenLookup = new ConcurrentDictionary<string, CancellationTokenSource>();

        public static IChangeToken CreateChangeToken(string[] ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var changeTokens = new List<IChangeToken>() { CreateChangeToken() };
            foreach (var id in ids)
            {
                changeTokens.Add(new CancellationChangeToken(_entityRegionTokenLookup.GetOrAdd(id, new CancellationTokenSource()).Token));
            }
            return new CompositeChangeToken(changeTokens);
        }

        public static void ExpireEntity(Notification entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (_entityRegionTokenLookup.TryRemove(entity.Id, out var token))
            {
                token.Cancel();
            }
        }
    }
}
