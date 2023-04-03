using System;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public sealed class NotificationLayoutBuilder
    {
        public NotificationLayoutBuilder(IServiceProvider serviceProvider, Type notificationLayoutType, Func<NotificationLayout> factory = null)
        {
            ServiceProvider = serviceProvider;
            NotificationLayout = factory != null ? factory() : Activator.CreateInstance(notificationLayoutType) as NotificationLayout;
        }

        public NotificationLayout NotificationLayout { get; set; } 

        public IServiceProvider ServiceProvider { get; private set; }


        public NotificationLayoutBuilder WithParams(string name, string template)
        {
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentNullException(nameof(template));
            }

            NotificationLayout.Name = name;
            NotificationLayout.Template = template;

            return this;
        }

        public NotificationLayout Build()
        {
            return NotificationLayout.Clone() as NotificationLayout;
        }
    }
}
