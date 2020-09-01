using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Caching;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationSearchService : INotificationSearchService
    {
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly INotificationService _notificationService;
        private readonly IPlatformMemoryCache _platformMemoryCache;

        public NotificationSearchService(Func<INotificationRepository> repositoryFactory, INotificationService notificationService,
            IPlatformMemoryCache platformMemoryCache)
        {
            _repositoryFactory = repositoryFactory;
            _notificationService = notificationService;
            _platformMemoryCache = platformMemoryCache;
        }

        public async Task<NotificationSearchResult> SearchNotificationsAsync(NotificationSearchCriteria criteria)
        {
            var cacheKey = CacheKey.With(GetType(), nameof(SearchNotificationsAsync), criteria.GetCacheKey());
            var notificationSearchResult = await _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async (cacheEntry) =>
            {
                cacheEntry.AddExpirationToken(NotificationSearchCacheRegion.CreateChangeToken());

                var result = AbstractTypeFactory<NotificationSearchResult>.TryCreateInstance();

                var sortInfos = BuildSortExpression(criteria);

                using (var repository = _repositoryFactory())
                {
                    //Optimize performance and CPU usage
                    repository.DisableChangesTracking();

                    var query = BuildQuery(repository, criteria, sortInfos);
                    result.TotalCount = await query.CountAsync();

                    if (criteria.Take > 0)
                    {
                        var notificationIds = await query.OrderBySortInfos(sortInfos).ThenBy(x => x.Id)
                                                         .Select(x => x.Id)
                                                         .Skip(criteria.Skip).Take(criteria.Take)
                                                         .ToArrayAsync();
                        var unorderedResults = await _notificationService.GetByIdsAsync(notificationIds, criteria.ResponseGroup);
                        result.Results = unorderedResults.OrderBy(x => Array.IndexOf(notificationIds, x.Id)).ToArray();

                        foreach (var notification in result.Results)
                        {
                            notification.ReduceDetails(criteria.ResponseGroup);
                        }
                    }
                }

                return result;
            });

            return notificationSearchResult.Clone() as NotificationSearchResult;
        }

        protected virtual IQueryable<NotificationEntity> BuildQuery(INotificationRepository repository, NotificationSearchCriteria criteria, IEnumerable<SortInfo> sortInfos)
        {
            var query = repository.Notifications;

            if (!string.IsNullOrEmpty(criteria.NotificationType))
            {
                var notificationType = GetNotificationType(criteria.NotificationType);
                query = query.Where(x => x.Type == notificationType);
            }

            query = query.Where(x => x.TenantId == criteria.TenantId);
            query = query.Where(x => x.TenantType == criteria.TenantType);

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                query = query.Where(x => x.Type.Contains(criteria.Keyword));
            }

            if (criteria.IsActive)
            {
                query = query.Where(x => x.IsActive);
            }

            query = query.OrderBySortInfos(sortInfos);
            return query;
        }

        protected virtual IList<SortInfo> BuildSortExpression(NotificationSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;
            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos = new[]
                {
                    new SortInfo
                    {
                        SortColumn = nameof(NotificationEntity.Type)
                    }
                };
            }
            return sortInfos;
        }

        protected virtual string GetNotificationType(string notificationType)
        {
            var typeInfo = AbstractTypeFactory<Notification>.FindTypeInfoByName(notificationType);
            var notification = GetTransientNotifications()
                               .FirstOrDefault(x => (typeInfo != null && x.GetType().Equals(typeInfo.Type))
                                                  || x.Alias.EqualsInvariant(notificationType));
            return notification?.Type;
        }

        private Notification[] GetTransientNotifications()
        {
            var cacheKey = CacheKey.With(GetType(), nameof(GetTransientNotifications));
            return _platformMemoryCache.GetOrCreateExclusive(cacheKey, (cacheEntry) =>
            {
                cacheEntry.AddExpirationToken(NotificationTypesCacheRegion.CreateChangeToken());
                return AbstractTypeFactory<Notification>.AllTypeInfos.Select(x =>
                {
                    return AbstractTypeFactory<Notification>.TryCreateInstance(x.Type.Name);
                }).ToArray();
            });

        }
    }
}
