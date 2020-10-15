using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.NotificationsModule.Core.Events;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Validation;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Exceptions;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationMessageService : INotificationMessageService
    {
        private readonly Func<INotificationRepository> _repositoryFactory;
        private readonly IEventPublisher _eventPublisher;

        public NotificationMessageService(Func<INotificationRepository> repositoryFactory, IEventPublisher eventPublisher)
        {
            _repositoryFactory = repositoryFactory;
            _eventPublisher = eventPublisher;
        }

        public async Task<NotificationMessage[]> GetNotificationsMessageByIds(string[] ids)
        {
            using (var repository = _repositoryFactory())
            {
                //Optimize performance and CPU usage
                repository.DisableChangesTracking();

                var messages = await repository.GetMessagesByIdsAsync(ids);
                return messages.Select(x => x.ToModel(AbstractTypeFactory<NotificationMessage>.TryCreateInstance($"{x.Kind}Message"))).ToArray();
            }
        }

        public async Task SaveNotificationMessagesAsync(NotificationMessage[] messages)
        {
            var validationResult = ValidateMessageProperties(messages.ToArray());
            await InnerSaveNotificationMessagesAsync(messages);

            if (!validationResult)
            {
                throw new PlatformException("There are validation errors. Look at notification feeds.");
            }
        }

        private async Task InnerSaveNotificationMessagesAsync(NotificationMessage[] messages)
        {
            var changedEntries = new List<GenericChangedEntry<NotificationMessage>>();
            var pkMap = new PrimaryKeyResolvingMap();

            using (var repository = _repositoryFactory())
            {
                var existingMessageEntities = await repository.GetMessagesByIdsAsync(messages.Select(m => m.Id).ToArray());
                foreach (var message in messages)
                {
                    var originalEntity = existingMessageEntities.FirstOrDefault(n => n.Id.Equals(message.Id));
                    var modifiedEntity = AbstractTypeFactory<NotificationMessageEntity>.TryCreateInstance($"{message.Kind}MessageEntity").FromModel(message, pkMap);

                    if (originalEntity != null)
                    {
                        /// This extension is allow to get around breaking changes is introduced in EF Core 3.0 that leads to throw
                        /// Database operation expected to affect 1 row(s) but actually affected 0 row(s) exception when trying to add the new children entities with manually set keys
                        /// https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-3.0/breaking-changes#detectchanges-honors-store-generated-key-values
                        repository.TrackModifiedAsAddedForNewChildEntities(originalEntity);

                        changedEntries.Add(new GenericChangedEntry<NotificationMessage>(message, originalEntity.ToModel(AbstractTypeFactory<NotificationMessage>.TryCreateInstance()), EntryState.Modified));
                        modifiedEntity?.Patch(originalEntity);
                    }
                    else
                    {
                        repository.Add(modifiedEntity);
                        changedEntries.Add(new GenericChangedEntry<NotificationMessage>(message, EntryState.Added));
                    }
                }

                await _eventPublisher.Publish(new NotificationMessageChangingEvent(changedEntries));
                await repository.UnitOfWork.CommitAsync();
                pkMap.ResolvePrimaryKeys();
                await _eventPublisher.Publish(new NotificationMessageChangedEvent(changedEntries));
            }
        }

        private bool ValidateMessageProperties(NotificationMessage[] messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            bool result = true;

            if (messages.Any())
            {
                var validator = AbstractTypeFactory<NotificationMessageValidator>.TryCreateInstance();
                var validationResult = messages.Where(m => m.Status == NotificationMessageStatus.Pending).Select(m =>
                {
                    var result = validator.Validate(m);
                    if (!result.IsValid)
                    {
                        m.LastSendError = string.Join(Environment.NewLine, result.Errors.Select(e => e.ErrorMessage));
                        m.Status = NotificationMessageStatus.Error;
                    }
                    return result;
                }).ToArray();

                result = validationResult.All(x => x.IsValid);
            }

            return result;
        }
    }
}
