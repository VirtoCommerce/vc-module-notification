using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VirtoCommerce.NotificationsModule.Core.Events;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Caching;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutService : CrudService<NotificationLayout, NotificationLayoutEntity, NotificationLayoutChangingEvent, NotificationLayoutChangedEvent>, INotificationLayoutService
    {
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly IPlatformMemoryCache _platformMemoryCache;
        private readonly INotificationLayoutRegistrar _layoutRegistrar;

        public NotificationLayoutService(
            Func<INotificationRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            INotificationLayoutRegistrar layoutRegistrar)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _repositoryFactory = repositoryFactory;
            _platformMemoryCache = platformMemoryCache;
            _layoutRegistrar = layoutRegistrar;
        }

        public override async Task<IList<NotificationLayout>> GetAsync(IList<string> ids, string responseGroup = null, bool clone = true)
        {
            var result = await base.GetAsync(ids, responseGroup, clone);

            // A predefined layout is referenced by its name (id == name). Resolve such ids to the DB override
            // of that layout if there is one, otherwise to the in-memory version.
            var foundIds = result.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var predefinedNames = ids.Where(id => !foundIds.Contains(id) && _layoutRegistrar.GetByName(id) != null).ToList();

            if (predefinedNames.Count == 0)
            {
                return result;
            }

            var overrideIds = await GetPredefinedOverrideIdsAsync();
            var overrides = await base.GetAsync(
                predefinedNames.Where(overrideIds.ContainsKey).Select(x => overrideIds[x]).ToList(),
                responseGroup,
                clone);

            foreach (var name in predefinedNames)
            {
                var layout = overrides.FirstOrDefault(x => x.Name.EqualsIgnoreCase(name));
                if (layout == null)
                {
                    // Always clone predefined layouts — registrar objects are shared singletons
                    layout = (NotificationLayout)_layoutRegistrar.GetByName(name).Clone();
                    layout.Id = name;
                    layout.IsPredefined = true;
                }

                result.Add(layout);
            }

            return result;
        }

        public override async Task DeleteAsync(IList<string> ids, bool softDelete = false)
        {
            IList<NotificationLayoutEntity> deletedLayouts;
            using (var repository = _repositoryFactory())
            {
                deletedLayouts = await repository.GetNotificationLayoutsByIdsAsync(ids);
            }

            await base.DeleteAsync(ids, softDelete);

            if (deletedLayouts.Count > 0)
            {
                await DetachTemplatesAsync(deletedLayouts);
            }
        }

        // Called during GetByIdsNoCache before the model is placed into the cache.
        // Safe to mutate here — the object is freshly created, not yet shared.
        protected override NotificationLayout ProcessModel(string responseGroup, NotificationLayoutEntity entity, NotificationLayout model)
        {
            model.IsPredefined = _layoutRegistrar.GetByName(model.Name) != null;
            return base.ProcessModel(responseGroup, entity, model);
        }

        protected override async Task BeforeSaveChanges(IList<NotificationLayout> models)
        {
            // Resolve synthetic ID for predefined layouts being saved for the first time.
            // Predefined layouts use Name as synthetic Id (id == name convention).
            // On save, resolve to the existing DB UUID (update) or null (insert).
            foreach (var layout in models.Where(x => x.Id == x.Name && _layoutRegistrar.GetByName(x.Name) != null))
            {
                using var repository = _repositoryFactory();
                var existing = repository.NotificationLayouts
                    .Where(x => x.Name == layout.Name)
                    .Select(x => new { x.Id })
                    .FirstOrDefault();

                layout.Id = existing?.Id;
            }

            await base.BeforeSaveChanges(models);
        }

        protected override Task<IList<NotificationLayoutEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((INotificationRepository)repository).GetNotificationLayoutsByIdsAsync(ids);
        }

        /// <summary>
        /// Returns DB ids of the layouts that override a predefined layout, by layout name.
        /// </summary>
        protected virtual Task<Dictionary<string, string>> GetPredefinedOverrideIdsAsync()
        {
            var cacheKey = CacheKey.With(GetType(), nameof(GetPredefinedOverrideIdsAsync));

            return _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async cacheEntry =>
            {
                cacheEntry.AddExpirationToken(GenericSearchCachingRegion<NotificationLayout>.CreateChangeToken());

                var predefinedNames = _layoutRegistrar.AllRegisteredLayouts.Select(x => x.Name).ToList();

                using var repository = _repositoryFactory();
                var overrides = await repository.NotificationLayouts
                    .Where(x => predefinedNames.Contains(x.Name))
                    .Select(x => new { x.Name, x.Id })
                    .ToListAsync();

                return overrides
                    .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(x => x.Key, x => x.First().Id, StringComparer.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// Templates that pointed to a deleted layout fall back to the predefined layout with the same name,
        /// or to no layout (the default one) when there is none. This replaces the former ON DELETE SET NULL foreign key.
        /// </summary>
        protected virtual async Task DetachTemplatesAsync(IList<NotificationLayoutEntity> deletedLayouts)
        {
            var deletedIds = deletedLayouts.Select(x => x.Id).ToList();

            using var repository = _repositoryFactory();
            var templates = await repository.Notifications
                .SelectMany(x => x.Templates)
                .OfType<EmailNotificationTemplateEntity>()
                .Where(x => deletedIds.Contains(x.NotificationLayoutId))
                .ToListAsync();

            if (templates.Count == 0)
            {
                return;
            }

            foreach (var template in templates)
            {
                var deletedLayout = deletedLayouts.First(x => x.Id.EqualsIgnoreCase(template.NotificationLayoutId));
                template.NotificationLayoutId = _layoutRegistrar.GetByName(deletedLayout.Name)?.Name;
            }

            await repository.UnitOfWork.CommitAsync();

            NotificationCacheRegion.ExpireRegion();
            NotificationSearchCacheRegion.ExpireRegion();
        }
    }
}
