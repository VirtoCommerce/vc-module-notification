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
    public class NotificationMessageSearchService(
        Func<INotificationRepository> repositoryFactory,
        INotificationMessageService messageService)
        : INotificationMessageSearchService
    {
        public async Task<NotificationMessageSearchResult> SearchMessageAsync(
            NotificationMessageSearchCriteria criteria)
        {
            var result = AbstractTypeFactory<NotificationMessageSearchResult>.TryCreateInstance();

            using var repository = repositoryFactory();
            repository.DisableChangesTracking();

            result.Results = new List<NotificationMessage>();
            var query = BuildQuery(repository, criteria);
            var needExecuteCount = criteria.Take == 0;

            if (criteria.Take > 0)
            {
                var messageIds = await query
                    .OrderBySortInfos(BuildSortExpression(criteria)).ThenBy(x => x.Id)
                    .Select(x => x.Id)
                    .Skip(criteria.Skip).Take(criteria.Take)
                    .ToArrayAsync();

                var unorderedResults = await messageService.GetNotificationsMessageByIds(messageIds);
                result.Results = unorderedResults.OrderBy(x => Array.IndexOf(messageIds, x.Id)).ToList();
                result.TotalCount = messageIds.Length;

                if (criteria.Skip > 0 || result.TotalCount == criteria.Take)
                {
                    needExecuteCount = true;
                }
            }

            if (needExecuteCount)
            {
                result.TotalCount = await query.CountAsync();
            }


            return result;
        }

        protected virtual IQueryable<NotificationMessageEntity> BuildQuery(
            INotificationRepository repository,
            NotificationMessageSearchCriteria criteria)
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
                        (((EmailNotificationMessageEntity)x).To != null &&
                         ((EmailNotificationMessageEntity)x).To.Contains(criteria.Keyword)) ||
                        (((EmailNotificationMessageEntity)x).From != null &&
                         ((EmailNotificationMessageEntity)x).From.Contains(criteria.Keyword)) ||
                        (((EmailNotificationMessageEntity)x).Subject != null &&
                         ((EmailNotificationMessageEntity)x).Subject.Contains(criteria.Keyword)) ||
                        (((EmailNotificationMessageEntity)x).CC != null &&
                         ((EmailNotificationMessageEntity)x).CC.Contains(criteria.Keyword)) ||
                        (((EmailNotificationMessageEntity)x).BCC != null &&
                         ((EmailNotificationMessageEntity)x).BCC.Contains(criteria.Keyword)) ||
                        (((EmailNotificationMessageEntity)x).Body != null &&
                         ((EmailNotificationMessageEntity)x).Body.Contains(criteria.Keyword)))));
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
