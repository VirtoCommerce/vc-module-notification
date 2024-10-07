using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationMessageSearchService : INotificationMessageSearchService
    {
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly INotificationMessageService _messageService;

        public NotificationMessageSearchService(Func<INotificationRepository> repositoryFactory, INotificationMessageService messageService)
        {
            _repositoryFactory = repositoryFactory;
            _messageService = messageService;
        }

        public async Task<NotificationMessageSearchResult> SearchMessageAsync(NotificationMessageSearchCriteria criteria)
        {
            var result = new NotificationMessageSearchResult();

            using var repository = _repositoryFactory();
            repository.DisableChangesTracking();

            result.Results = new List<NotificationMessage>();
            var query = BuildQuery(repository, criteria);
            var sortInfos = BuildSortExpression(criteria);

            result.TotalCount = await query.CountAsync();

            if (criteria.Take > 0)
            {
                var messageIds = await query.OrderBySortInfos(sortInfos).ThenBy(x => x.Id)
                    .Select(x => x.Id)
                    .Skip(criteria.Skip).Take(criteria.Take)
                    .ToArrayAsync();

                var unorderedResults = await _messageService.GetNotificationsMessageByIds(messageIds);
                result.Results = unorderedResults.OrderBy(x => Array.IndexOf(messageIds, x.Id)).ToList();
            }

            return result;
        }

        protected virtual IQueryable<NotificationMessageEntity> BuildQuery(INotificationRepository repository, NotificationMessageSearchCriteria criteria)
        {
            var query = repository.NotificationMessages;

            if (!criteria.ObjectIds.IsNullOrEmpty())
            {
                query = query.Where(n => criteria.ObjectIds.Contains(n.TenantId));
            }

            if (!criteria.ObjectTypes.IsNullOrEmpty())
            {
                query = query.Where(n => criteria.ObjectTypes.Contains(n.TenantType));
            }

            if (!string.IsNullOrEmpty(criteria.NotificationType))
            {
                query = query.Where(x => x.NotificationType == criteria.NotificationType);
            }

            if (!string.IsNullOrEmpty(criteria.Status))
            {
                query = query.Where(x => x.Status == criteria.Status);
            }

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                query = query.Where(x =>
                    x.Status != null && x.Status.Contains(criteria.Keyword) ||
                    x.NotificationType != null && x.NotificationType.Contains(criteria.Keyword) ||
                    x.LastSendError != null && x.LastSendError.Contains(criteria.Keyword) ||
                    (x is EmailNotificationMessageEntity && (
                        ((EmailNotificationMessageEntity)x).To.Contains(criteria.Keyword) ||
                        ((EmailNotificationMessageEntity)x).From.Contains(criteria.Keyword) ||
                        ((EmailNotificationMessageEntity)x).Subject.Contains(criteria.Keyword) ||
                        ((EmailNotificationMessageEntity)x).CC.Contains(criteria.Keyword) ||
                        ((EmailNotificationMessageEntity)x).BCC.Contains(criteria.Keyword) ||
                        ((EmailNotificationMessageEntity)x).Body.Contains(criteria.Keyword))));
            }

            return query;
        }

        protected virtual IList<SortInfo> BuildSortExpression(NotificationMessageSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;
            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos =
                [
                    new SortInfo
                    {
                        SortColumn = nameof(NotificationMessageEntity.CreatedDate),
                        SortDirection = SortDirection.Descending
                    }
                ];
            }
            return sortInfos;
        }

    }
}
