using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Events;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutService : CrudService<NotificationLayout, NotificationLayoutEntity, NotificationLayoutChangingEvent, NotificationLayoutChangedEvent>, INotificationLayoutService
    {
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly INotificationLayoutRegistrar _layoutRegistrar;

        public NotificationLayoutService(
            Func<INotificationRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            INotificationLayoutRegistrar layoutRegistrar)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _repositoryFactory = repositoryFactory;
            _layoutRegistrar = layoutRegistrar;
        }

        public override async Task<IList<NotificationLayout>> GetAsync(IList<string> ids, string responseGroup = null, bool clone = true)
        {
            var result = await base.GetAsync(ids, responseGroup, clone);

            // For IDs not found in DB, try predefined fallback (id == predefined layout name)
            var foundIds = result.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var id in ids.Where(id => !foundIds.Contains(id)))
            {
                var predefined = _layoutRegistrar.GetByName(id);
                if (predefined != null)
                {
                    // Always clone predefined layouts — registrar objects are shared singletons
                    var layout = (NotificationLayout)predefined.Clone();
                    layout.Id = id;
                    layout.IsPredefined = true;
                    result.Add(layout);
                }
            }

            return result;
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
    }
}
