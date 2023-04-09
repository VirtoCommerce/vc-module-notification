using System.Collections.Generic;
using System.IO;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutRegistrar : INotificationLayoutRegistrar
    {
        private readonly IList<NotificationLayout> _layouts = new List<NotificationLayout>();

        public IEnumerable<NotificationLayout> AllRegisteredNotificationsLayout
        {
            get
            {
                return _layouts;
            }
        }

        public NotificationLayoutRegistrar()
        {
        }

        public void RegisterNotificationLayoutWithParams(string name, string template)
        {
            RegisterNotificationLayout(name, template);
        }

        public void RegisterNotificationLayoutWithTemplateFromPath(string name, string path)
        {
            var template = LoadTemplateNotificationLayout(name, path);

            RegisterNotificationLayout(name, template);
        }

        private void RegisterNotificationLayout(string name, string template)
        {
            var existLayout = _layouts.FirstOrDefault(x => x.Name == name);

            if (existLayout == null)
            {
                var layout = new NotificationLayout();

                layout.Name = name;
                layout.Template = template;

                _layouts.Add(layout);
            }
            else
            {
                existLayout.Template = template;
            }
        }

        private string LoadTemplateNotificationLayout(string name, string path)
        {
            var result = string.Empty;
            if (Directory.Exists(path))
            {
                var file = Directory.GetFiles(path, $"{name}*.*", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (file != null)
                {
                    result = File.ReadAllText(file);
                }
            }

            return result;
        }
    }
}
