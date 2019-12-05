using System;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
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
                    if (result.PredefinedTemplates != null)
                    {
                        x.Templates = result.PredefinedTemplates.ToList();
                    }
                });

                var notification = _notificationSearchService.GetNotificationAsync<T>().GetAwaiter().GetResult();
                if (notification == null)
                {
                    _notificationService.SaveChangesAsync(new[] { AbstractTypeFactory<Notification>.TryCreateInstance(typeof(T).Name) }).GetAwaiter().GetResult();
                }

            }

            return result;
        }
    }
}
