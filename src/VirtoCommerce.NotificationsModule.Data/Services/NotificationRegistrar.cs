using System;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Caching;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationRegistrar : INotificationRegistrar
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationSearchService _notificationSearchService;

        public NotificationRegistrar(INotificationService notificationService, INotificationSearchService notificationSearchService)
        {
            _notificationService = notificationService;
            _notificationSearchService = notificationSearchService;
        }
        
        public NotificationBuilder RegisterNotification<T>(Func<Notification> factory = null) where T : Notification
        {
            var result = new NotificationBuilder();

            if (AbstractTypeFactory<Notification>.AllTypeInfos.All(t => t.Type != typeof(T)))
            {
                AbstractTypeFactory<Notification>.RegisterType<T>().WithFactory(factory).WithSetupAction((x) =>
                {
                    if (!result.PredefinedTemplates.IsNullOrEmpty())
                    {
                        x.Templates = result.PredefinedTemplates.ToList();
                    }
                });

                TryToSave<T>();
            }

            return result;
        }

        public NotificationBuilder OverrideNotificationType<OldType, NewType>(Func<Notification> factory = null) where OldType : Notification where NewType : Notification
        {
            var result = new NotificationBuilder();

            if (AbstractTypeFactory<Notification>.AllTypeInfos.All(t => t.Type != typeof(NewType)))
            {
                AbstractTypeFactory<Notification>.OverrideType<OldType, NewType>().WithFactory(factory).WithSetupAction((x) =>
                {
                    if (!result.PredefinedTemplates.IsNullOrEmpty())
                    {
                        x.Templates = result.PredefinedTemplates.ToList();
                    }
                });

                TryToSave<NewType>();
            }

            return result;
        }

        private void TryToSave<T>() where T : Notification
        {
            NotificationTypesCacheRegion.ExpireRegion();
            
            var notification = _notificationSearchService.GetNotificationAsync<T>().GetAwaiter().GetResult();

            if (notification == null)
            {
                _notificationService.SaveChangesAsync(new[] { AbstractTypeFactory<Notification>.TryCreateInstance(typeof(T).Name) }).GetAwaiter().GetResult();
            }
            else
            {
                //need to expire the region because not yeat add predefined templates
                NotificationSearchCacheRegion.ExpireRegion();
                NotificationCacheRegion.ExpireEntity(notification);
            }
        }
    }
}
