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
        private readonly INotificationTemplateLoader _templateLoader;

        public NotificationService(
            Func<INotificationRepository> repositoryFactory
            , IEventPublisher eventPublisher
            , INotificationTemplateLoader templateLoader
            , IPlatformMemoryCache platformMemoryCache)
        {
            _repositoryFactory = repositoryFactory;
            _eventPublisher = eventPublisher;
            _platformMemoryCache = platformMemoryCache;
            _templateLoader = templateLoader;
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

                    var entities = await repository.GetByIdsAsync(ids, responseGroup);
                    var notifications = entities?.Select(ToModel).ToArray();
                    //Load predefined notifications templates
                    if (notifications != null && EnumUtility.SafeParseFlags(responseGroup, NotificationResponseGroup.Full).HasFlag(NotificationResponseGroup.WithTemplates))
                    {
                        foreach (var notification in notifications)
                        {
                            var predefinedTemplates = _templateLoader.LoadTemplates(notification).ToList();
                            if (notification.Templates != null)
                            {
                                notification.Templates.AddRange(predefinedTemplates);
                            }
                            else
                            {
                                notification.Templates = predefinedTemplates;
                            }
                        }
                    }
                    return notifications;
                }
            });

            return result?.Select(x => x.Clone() as Notification).ToArray();
        }

        public async Task SaveChangesAsync(Notification[] notifications)
        {
            if (notifications != null && notifications.Any())
            {
                ValidateNotificationProperties(notifications);

                var pkMap = new PrimaryKeyResolvingMap();
                var changedEntries = new List<GenericChangedEntry<Notification>>();
                var changedEntities = new List<NotificationEntity>();

                using (var repository = _repositoryFactory())
                {
                    var existingNotificationEntities = await repository.GetByNotificationsAsync(notifications, NotificationResponseGroup.Full.ToString());
                    //Convert db entities to domain notification to be able compare them with passed as parameter
                    var existingNotifications = existingNotificationEntities?.Select(ToModel).ToArray();
                    var comparer = AnonymousComparer.Create((Notification x) => string.Join("-", x.Type, x.TenantIdentity.Id, x.TenantIdentity.Type));
                    foreach (var notification in notifications)
                    {
                        var modifiedEntity = AbstractTypeFactory<NotificationEntity>.TryCreateInstance($"{notification.Kind}Entity").FromModel(notification, pkMap);

                        var existingNotification = existingNotifications?.FirstOrDefault(x => comparer.Equals(x, notification));
                        if (existingNotification != null)
                        {
                            var originalEntity = existingNotificationEntities.First(n => n.Id.EqualsIgnoreCase(existingNotification.Id));

                            // This extension is allow to get around breaking changes is introduced in EF Core 3.0 that leads to throw
                            // Database operation expected to affect 1 row(s) but actually affected 0 row(s) exception when trying to add the new children entities with manually set keys
                            // https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-3.0/breaking-changes#detectchanges-honors-store-generated-key-values
                            repository.TrackModifiedAsAddedForNewChildEntities(originalEntity);

                            changedEntries.Add(new GenericChangedEntry<Notification>(notification, ToModel(originalEntity), EntryState.Modified));
                            modifiedEntity?.Patch(originalEntity);
                            changedEntities.Add(originalEntity);
                        }
                        else
                        {
                            //need to reset entity data for create notification with tenant based on the global notification
                            repository.Add(modifiedEntity.ResetEntityData());
                            changedEntries.Add(new GenericChangedEntry<Notification>(notification, EntryState.Added));
                            changedEntities.Add(modifiedEntity);
                        }
                    }

                    //Raise domain events
                    await _eventPublisher.Publish(new NotificationChangingEvent(changedEntries));
                    //Save changes in database
                    await repository.UnitOfWork.CommitAsync();
                    pkMap.ResolvePrimaryKeys();
                    ClearCache(notifications);

                    foreach (var (changedEntry, i) in changedEntries.Select((x, i) => (x, i)))
                    {
                        changedEntry.NewEntry = ToModel(changedEntities[i]);
                    }

                    await _eventPublisher.Publish(new NotificationChangedEvent(changedEntries));
                }
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

        private static Notification ToModel(NotificationEntity entity)
        {
            Notification result = null;
            var typeName = entity.Type;
            var typeInfo = AbstractTypeFactory<Notification>.FindTypeInfoByName(typeName);

            if (typeInfo != null)
            {
                result = AbstractTypeFactory<Notification>.TryCreateInstance(typeName);
                entity.ToModel(result);
            }

            return result ?? new UnregisteredNotification(typeName);
        }
    }
}
