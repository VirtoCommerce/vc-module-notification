using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using VirtoCommerce.NotificationsModule.Core.Events;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Caching;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Validation;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.NotificationsModule.Data.Services
{

    public class NotificationService : INotificationService
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly IPlatformMemoryCache _platformMemoryCache;

        public NotificationService(Func<INotificationRepository> repositoryFactory, IEventPublisher eventPublisher,
            IPlatformMemoryCache platformMemoryCache)
        {
            _repositoryFactory = repositoryFactory;
            _eventPublisher = eventPublisher;
            _platformMemoryCache = platformMemoryCache;
        }


        public async Task<Notification[]> GetByIdsAsync(string[] ids, string responseGroup = null)
        {
            var cacheKey = CacheKey.With(GetType(), nameof(GetByIdsAsync), string.Join("-", ids), responseGroup);
            return await _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async (cacheEntry) =>
            {
                cacheEntry.AddExpirationToken(NotificationCacheRegion.CreateChangeToken());

                using (var repository = _repositoryFactory())
                {
                    var notifications = await repository.GetByIdsAsync(ids, responseGroup);
                    return notifications.Select(n =>
                    {
                        return n.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(n.Type));
                    }).ToArray();
                }
            });
        }

        public async Task SaveChangesAsync(Notification[] notifications)
        {
            if (notifications != null && notifications.Any())
            {
                ValidateNotificationProperties(notifications);

                var pkMap = new PrimaryKeyResolvingMap();
                var changedEntries = new List<GenericChangedEntry<Notification>>();
                using (var repository = _repositoryFactory())
                {
                    var existingNotificationEntities = await repository.GetByIdsAsync(notifications.Select(m => m.Id).ToArray(), NotificationResponseGroup.Full.ToString());
                    foreach (var notification in notifications)
                    {
                        var originalEntity = existingNotificationEntities.FirstOrDefault(n => n.Id.Equals(notification.Id));
                        var modifiedEntity = AbstractTypeFactory<NotificationEntity>.TryCreateInstance($"{notification.Kind}Entity").FromModel(notification, pkMap);

                        if (originalEntity != null)
                        {
                            changedEntries.Add(new GenericChangedEntry<Notification>(notification, originalEntity.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance()), EntryState.Modified));
                            modifiedEntity?.Patch(originalEntity);
                        }
                        else
                        {
                            repository.Add(modifiedEntity);
                            changedEntries.Add(new GenericChangedEntry<Notification>(notification, EntryState.Added));
                        }
                    }

                    //Raise domain events
                    await _eventPublisher.Publish(new NotificationChangingEvent(changedEntries));
                    //Save changes in database
                    await repository.UnitOfWork.CommitAsync();
                    pkMap.ResolvePrimaryKeys();
                    await _eventPublisher.Publish(new NotificationChangedEvent(changedEntries));
                }

                NotificationCacheRegion.ExpireRegion();
            }
        }


        private void ValidateNotificationProperties(IEnumerable<Notification> notifications)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException(nameof(notifications));
            }
            var validator = new NotificationValidator();
            foreach (var notification in notifications)
            {
                validator.ValidateAndThrow(notification);
            }
        }
    }
}
