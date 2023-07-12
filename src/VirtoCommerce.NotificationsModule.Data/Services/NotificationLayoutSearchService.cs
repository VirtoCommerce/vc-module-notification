using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutSearchService : SearchService<NotificationLayoutSearchCriteria, NotificationLayoutSearchResult, NotificationLayout, NotificationLayoutEntity>, INotificationLayoutSearchService
    {
        public NotificationLayoutSearchService(
            Func<INotificationRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            ICrudService<NotificationLayout> service)
            : base(repositoryFactory, platformMemoryCache, service)
        {
        }

        protected override IQueryable<NotificationLayoutEntity> BuildQuery(IRepository repository, NotificationLayoutSearchCriteria criteria)
        {
            var query = ((INotificationRepository)repository).NotificationLayouts;

            if (!criteria.ObjectIds.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.ObjectIds.Contains(x.Id));
            }

            if (!criteria.Names.IsNullOrEmpty())
            {
                query = criteria.Names.Count == 1
                    ? query.Where(x => x.Name == criteria.Names.First())
                    : query.Where(x => criteria.Names.Contains(x.Name));
            }

            if (!criteria.Keyword.IsNullOrEmpty())
            {
                query = query.Where(x => x.Name.Contains(criteria.Keyword));
            }

            if (criteria.IsDefault.HasValue)
            {
                query = query.Where(x => x.IsDefault == criteria.IsDefault);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(NotificationLayoutSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;
            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos = new[]
                {
                    new SortInfo
                    {
                        SortColumn = nameof(NotificationLayoutEntity.Name)
                    }
                };
            }
            return sortInfos;
        }
    }
}
