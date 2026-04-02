using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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
        private readonly INotificationLayoutRegistrar _layoutRegistrar;

        public NotificationLayoutSearchService(
            Func<INotificationRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            INotificationLayoutService crudService,
            IOptions<CrudOptions> crudOptions,
            INotificationLayoutRegistrar layoutRegistrar)
            : base(repositoryFactory, platformMemoryCache, crudService, crudOptions)
        {
            _layoutRegistrar = layoutRegistrar;
        }

        public override async Task<NotificationLayoutSearchResult> SearchAsync(NotificationLayoutSearchCriteria criteria, bool clone = true)
        {
            // Find which predefined layouts already have a DB override.
            var predefinedNames = _layoutRegistrar.AllRegisteredLayouts.Select(x => x.Name).ToList();
            var overriddenNames = predefinedNames.Count > 0
                ? (await base.SearchAsync(
                    new NotificationLayoutSearchCriteria { Names = predefinedNames, Take = predefinedNames.Count }, clone: false))
                    .Results.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Build the list of predefined layouts not overridden in the DB, applying search filters.
            var predefinedLayouts = _layoutRegistrar.AllRegisteredLayouts
                .Where(x => !overriddenNames.Contains(x.Name))
                .Where(x => criteria.Names is not { Count: > 0 } || criteria.Names.Any(n => n.EqualsIgnoreCase(x.Name)))
                .Where(x => criteria.IsDefault == null || x.IsDefault == criteria.IsDefault)
                .Where(x => string.IsNullOrEmpty(criteria.Keyword) || x.Name.Contains(criteria.Keyword, StringComparison.OrdinalIgnoreCase))
                .Select(x =>
                {
                    var layout = (NotificationLayout)x.Clone();
                    layout.Id = x.Name;
                    layout.IsPredefined = true;
                    return layout;
                })
                .ToList();

            var predefinedCount = predefinedLayouts.Count;

            // Predefined layouts occupy the first slots in the virtual result set.
            // Adjust Skip/Take for the DB query accordingly.
            var adjustedSkip = Math.Max(0, criteria.Skip - predefinedCount);
            var predefinedOnThisPage = criteria.Skip < predefinedCount
                ? predefinedLayouts.Skip(criteria.Skip).Take(criteria.Take).ToList()
                : [];
            var dbTake = criteria.Take - predefinedOnThisPage.Count;

            var dbCriteria = criteria.CloneTyped();
            dbCriteria.Skip = adjustedSkip;
            dbCriteria.Take = Math.Max(0, dbTake);

            var searchResult = await base.SearchAsync(dbCriteria, clone);

            // Combine: predefined first, then DB results
            var combinedResults = new List<NotificationLayout>(predefinedOnThisPage);
            combinedResults.AddRange(searchResult.Results);

            searchResult.Results = combinedResults;
            searchResult.TotalCount += predefinedCount;

            return searchResult;
        }

        protected override IQueryable<NotificationLayoutEntity> BuildQuery(IRepository repository, NotificationLayoutSearchCriteria criteria)
        {
            var query = ((INotificationRepository)repository).NotificationLayouts;

            if (!criteria.ObjectIds.IsNullOrEmpty())
            {
                query = criteria.ObjectIds.Count == 1
                    ? query.Where(x => x.Id == criteria.ObjectIds.First())
                    : query.Where(x => criteria.ObjectIds.Contains(x.Id));
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
