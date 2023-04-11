using System.Collections.Generic;
using System.IO;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutRegistrar : INotificationLayoutRegistrar
    {
        private readonly IList<NotificationLayout> _layouts = new List<NotificationLayout>();

        public IEnumerable<NotificationLayout> AllRegisteredLayouts => _layouts;

        public void RegisterLayout(string name, string template)
        {
            var layout = _layouts.FirstOrDefault(x => x.Name.EqualsInvariant(name));

            if (layout == null)
            {
                layout = AbstractTypeFactory<NotificationLayout>.TryCreateInstance();
                layout.Name = name;

                _layouts.Add(layout);
            }

            layout.Template = template;
        }

        public void RegisterLayoutWithTemplateFromPath(string name, string path)
        {
            RegisterLayout(name, LoadTemplateNotificationLayout(name, path));
        }

        private static string LoadTemplateNotificationLayout(string name, string path)
        {
            string result = null;
            if (Directory.Exists(path))
            {
                var file = Directory.EnumerateFiles(path, $"{name}*.*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (file != null)
                {
                    result = File.ReadAllText(file);
                }
            }

            return result;
        }
    }
}
