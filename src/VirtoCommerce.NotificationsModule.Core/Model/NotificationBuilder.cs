using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public sealed class NotificationBuilder
    {
        public NotificationBuilder(IServiceProvider serviceProvider, Type notificationType, Func<Notification> factory = null)
        {
            ServiceProvider = serviceProvider;
            Notification = factory != null ? factory() : Activator.CreateInstance(notificationType) as Notification;
            //by set null we prevent update existing notifications with one of logical values
            Notification.IsActive = null;
        }

        public Notification Notification { get; private set; }

        public IServiceProvider ServiceProvider { get; private set; }

        public NotificationBuilder SetIsActive(bool isActive)
        {
            Notification.IsActive = isActive;
            return this;
        }


        public NotificationBuilder WithTemplates(params NotificationTemplate[] templates)
        {
            if (templates == null)
            {
                throw new ArgumentNullException(nameof(templates));
            }
            if (Notification.Templates == null)
            {
                Notification.Templates = new List<NotificationTemplate>();
            }
            foreach (var template in templates)
            {
                template.IsPredefined = true;
                Notification.Templates.Add(template);
            }
            return this;
        }

        public NotificationBuilder WithLayout(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return this;
            }

            var notificationLayoutSearchService = ServiceProvider.GetService<ISearchService<NotificationLayoutSearchCriteria, NotificationLayoutSearchResult, NotificationLayout>>();

            var layouts = notificationLayoutSearchService.SearchAsync(new NotificationLayoutSearchCriteria
            {
                Keyword = name,
                Take = 1,
                Skip = 0,
            }).GetAwaiter().GetResult();

            if (layouts.Results.Any())
            {
                foreach (var template in Notification.Templates)
                {
                    if (template is EmailNotificationTemplate emailTemplate)
                    {
                        emailTemplate.NotificationLayoutId = layouts.Results.FirstOrDefault()?.Id;
                    }
                }
            }

            return this;
        }

        public Notification Build()
        {
            return Notification.Clone() as Notification;
        }
    }
}
