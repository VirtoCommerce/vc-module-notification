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
            return await _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async (cacheEntry) =>
            {
                cacheEntry.AddExpirationToken(NotificationCacheRegion.CreateChangeToken());

                var result = AbstractTypeFactory<NotificationSearchResult>.TryCreateInstance();

                var sortInfos = BuildSortExpression(criteria);

                using (var repository = _repositoryFactory())
                {
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

                        GetTransientNotifications(criteria, result);

                        foreach (var notification in result.Results)
                        {
                            notification.ReduceDetails(criteria.ResponseGroup);
                        }
                    }
                }

                return result;
            });
        }

        protected virtual IQueryable<NotificationEntity> BuildQuery(INotificationRepository repository, NotificationSearchCriteria criteria, IEnumerable<SortInfo> sortInfos)
        {
            var query = repository.Notifications;

            if (!string.IsNullOrEmpty(criteria.NotificationType))
            {
                query = query.Where(x => x.Type == GetNotificationType(criteria.NotificationType));
            }

            if (!string.IsNullOrEmpty(criteria.TenantId))
            {
                query = query.Where(x => x.TenantId == criteria.TenantId);
            }

            if (!string.IsNullOrEmpty(criteria.TenantType))
            {
                query = query.Where(x => x.TenantType == criteria.TenantType);
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

        protected virtual void GetTransientNotifications(NotificationSearchCriteria criteria, NotificationSearchResult result)
        {
            var transientNotificationsQuery = GetTransientNotifications();

            if (!string.IsNullOrEmpty(criteria.NotificationType))
            {
                transientNotificationsQuery = transientNotificationsQuery.Where(x => x.Type.EqualsInvariant(criteria.NotificationType));
            }

            var allPersistentProvidersTypes = result.Results.Select(x => x.Type).Distinct();

            transientNotificationsQuery = transientNotificationsQuery.Where(x => !allPersistentProvidersTypes.Contains(x.Type));

            result.TotalCount += transientNotificationsQuery.Count();

            //no skip transient notifications
            var transientNotifications = transientNotificationsQuery.Take(criteria.Take - result.TotalCount).ToList();

            result.Results = result.Results.Concat(transientNotifications)
                                           .AsQueryable()
                                           .OrderBySortInfos(BuildSortExpression(criteria))
                                           .ToList();
        }

        protected virtual string GetNotificationType(string notificationType)
        {
            return GetTransientNotifications()
                .FirstOrDefault(x => x.Type.EqualsInvariant(notificationType) || x.Alias.EqualsInvariant(notificationType))
                .Type;
        }

        private IEnumerable<Notification> GetTransientNotifications()
        {
            var cacheKey = CacheKey.With(GetType(), "GetTransientNotifications");
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
