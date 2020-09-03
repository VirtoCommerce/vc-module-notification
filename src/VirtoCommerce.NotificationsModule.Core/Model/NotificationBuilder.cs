using System;
using System.Collections.Generic;

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
                template.IsReadonly = true;
                template.IsPredefined = true;
                Notification.Templates.Add(template);
            }
            return this;
        }

        public Notification Build()
        {
            return Notification.Clone() as Notification;
        }
    }
}
