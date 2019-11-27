using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationSearchService : INotificationSearchService
    {
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly INotificationService _notificationService;

        public NotificationSearchService(Func<INotificationRepository> repositoryFactory, INotificationService notificationService)
        {
            _repositoryFactory = repositoryFactory;
            _notificationService = notificationService;
        }

        public async Task<NotificationSearchResult> SearchNotificationsAsync(NotificationSearchCriteria criteria)
        {
            var result = AbstractTypeFactory<NotificationSearchResult>.TryCreateInstance();

            var allTransientNotifications = GetAllTransientNotifications(criteria);
            result.TotalCount = allTransientNotifications.Count();

            var batchTransientNotifications = allTransientNotifications.Skip(criteria.Skip).Take(criteria.Take).ToArray();

            if (!batchTransientNotifications.IsNullOrEmpty())
            {
                criteria.NotificationTypes = batchTransientNotifications.Select(x => x.Type).ToArray();
                criteria.Skip = 0; //that need to search persisted notifications
                result.Results = await GetPersistedNotifications(criteria);

                criteria.Take -= result.Results.Count();
            }

            if (criteria.Take > 0)
            {
                var allPersistedProvidersTypes = result.Results.Select(x => x.GetType()).Distinct();
                var transientNotificationsQuery = batchTransientNotifications.Where(x => !allPersistedProvidersTypes.Contains(x.GetType()));

                result.Results = result.Results.Concat(transientNotificationsQuery)
                    .AsQueryable()
                    .OrderBySortInfos(BuildSortExpression(criteria)).ToList();
            }

            return result;
        }

        protected virtual Notification[] GetAllTransientNotifications(NotificationSearchCriteria criteria)
        {
            var transientNotificationsQuery = AbstractTypeFactory<Notification>.AllTypeInfos.Select(x =>
            {
                return AbstractTypeFactory<Notification>.TryCreateInstance(x.Type.Name);
            }).AsQueryable();

            if (!string.IsNullOrEmpty(criteria.NotificationType))
            {
                transientNotificationsQuery = transientNotificationsQuery.Where(x => x.Type.EqualsInvariant(criteria.NotificationType)
                                                                                    || x.Alias.EqualsInvariant(criteria.NotificationType));
            }

            transientNotificationsQuery = transientNotificationsQuery.Where(x => !x.Kind.EqualsInvariant(x.Type))
                                                                     .OrderBySortInfos(BuildSortExpression(criteria));

            var transientNotifications = transientNotificationsQuery.Distinct().ToArray();
            foreach (var transientNotification in transientNotifications)
            {
                transientNotification.ReduceDetails(criteria.ResponseGroup);
            }

            return transientNotifications;
        }

        protected virtual async Task<Notification[]> GetPersistedNotifications(NotificationSearchCriteria criteria)
        {
            var result = Array.Empty<Notification>();
            var sortInfos = BuildSortExpression(criteria);

            using (var repository = _repositoryFactory())
            {
                var query = BuildQuery(repository, criteria, sortInfos);

                if (criteria.Take > 0)
                {
                    var notificationIds = await query.OrderBySortInfos(sortInfos).ThenBy(x => x.Id)
                                                     .Select(x => x.Id)
                                                     .Skip(criteria.Skip).Take(criteria.Take)
                                                     .ToArrayAsync();
                    var unorderedResults = await _notificationService.GetByIdsAsync(notificationIds, criteria.ResponseGroup);
                    result = unorderedResults.OrderBy(x => Array.IndexOf(notificationIds, x.Id)).ToArray();

                    foreach (var notification in result)
                    {
                        notification.ReduceDetails(criteria.ResponseGroup);
                    }
                }
            }

            return result;
        }

        protected virtual IQueryable<NotificationEntity> BuildQuery(INotificationRepository repository, NotificationSearchCriteria criteria, IEnumerable<SortInfo> sortInfos)
        {
            var query = repository.Notifications;

            if (!criteria.NotificationTypes.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.NotificationTypes.Contains(x.Type));
            }
            else if (!string.IsNullOrEmpty(criteria.NotificationType))
            {
                query = query.Where(x => x.Type == criteria.NotificationType);
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
    }
}
