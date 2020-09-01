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
using VirtoCommerce.Platform.Data.Infrastructure;

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
            var result = await _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async (cacheEntry) =>
            {
                //It is so important to generate change tokens for all ids even for not existing objects to prevent an issue
                //with caching of empty results for non - existing objects that have the infinitive lifetime in the cache
                //and future unavailability to create objects with these ids.
                cacheEntry.AddExpirationToken(NotificationCacheRegion.CreateChangeToken(ids));

                using (var repository = _repositoryFactory())
                {
                    //Optimize performance and CPU usage
                    repository.DisableChangesTracking();

                    var notifications = await repository.GetByIdsAsync(ids, responseGroup);
                    return notifications.Select(n => n.ToModel(CreateNotification(n.Type, new UnregisteredNotification()))).ToArray();
                }
            });

            return result.Select(x => x.Clone() as Notification).ToArray();
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
                    var existingNotificationEntities = await repository.GetByNotificationsAsync(notifications, NotificationResponseGroup.Full.ToString());
                    var existingNotifications = existingNotificationEntities.Select(e => e.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance(e.Type))).ToArray();
                    var comparer = AnonymousComparer.Create((Notification x) => string.Join("-", x.Type, x.TenantIdentity.Id, x.TenantIdentity.Type));
                    foreach (var notification in notifications)
                    {
                        var existingNotification = existingNotifications.FirstOrDefault(x => comparer.Equals(x, notification));
                        var originalEntity = existingNotificationEntities.FirstOrDefault(n => n.Id.EqualsInvariant(existingNotification.Id));
                        var modifiedEntity = AbstractTypeFactory<NotificationEntity>.TryCreateInstance($"{notification.Kind}Entity").FromModel(notification, pkMap);

                        if (originalEntity != null)
                        {
                            /// This extension is allow to get around breaking changes is introduced in EF Core 3.0 that leads to throw
                            /// Database operation expected to affect 1 row(s) but actually affected 0 row(s) exception when trying to add the new children entities with manually set keys
                            /// https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-3.0/breaking-changes#detectchanges-honors-store-generated-key-values
                            repository.TrackModifiedAsAddedForNewChildEntities(originalEntity);

                            changedEntries.Add(new GenericChangedEntry<Notification>(notification, originalEntity.ToModel(AbstractTypeFactory<Notification>.TryCreateInstance()), EntryState.Modified));
                            modifiedEntity?.Patch(originalEntity);
                        }
                        else
                        {
                            //need to reset entity data for create notification with tenant based on the global notification
                            repository.Add(modifiedEntity.ResetEntityData());
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

                ClearCache(notifications);
            }
        }


        private void ClearCache(Notification[] notifications)
        {
            NotificationSearchCacheRegion.ExpireRegion();

            foreach (var item in notifications)
            {
                NotificationCacheRegion.ExpireEntity(item);
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

        private static Notification CreateNotification(string typeName, Notification defaultObj)
        {
            var result = defaultObj;
            var typeInfo = AbstractTypeFactory<Notification>.FindTypeInfoByName(typeName);
            if (typeInfo != null)
            {
                result = AbstractTypeFactory<Notification>.TryCreateInstance(typeName);
            }
            return result;
        }
    }
}
