using System.Collections.Generic;
using System.IO;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.NotificationsModule.Data.Services
{
    public class NotificationLayoutRegistrar : INotificationLayoutRegistrar
    {
        private readonly IList<NotificationLayout> _layouts = new List<NotificationLayout>();

        private readonly ICrudService<NotificationLayout> _layoutService;
        private readonly ISearchService<NotificationLayoutSearchCriteria, NotificationLayoutSearchResult, NotificationLayout> _layoutSearchService;

        public NotificationLayoutRegistrar(ICrudService<NotificationLayout> layoutService, ISearchService<NotificationLayoutSearchCriteria, NotificationLayoutSearchResult, NotificationLayout> layoutSearchService)
        {
            _layoutService = layoutService;
            _layoutSearchService = layoutSearchService;
        }

        public IEnumerable<NotificationLayout> AllRegisteredLayouts => _layouts;

        public void RegisterLayout(string name, string template, bool saveChanges = false)
        {
            var layout = _layouts.FirstOrDefault(x => x.Name.EqualsInvariant(name));

            if (layout == null)
            {
                layout = AbstractTypeFactory<NotificationLayout>.TryCreateInstance();
                layout.Name = name;

                _layouts.Add(layout);
            }

            layout.Template = template;

            if (saveChanges)
            {
                SaveChanges();
            }
        }

        public void RegisterLayoutWithTemplateFromPath(string name, string path, bool saveChanges = false)
        {
            RegisterLayout(name, LoadTemplateNotificationLayout(name, path), saveChanges);
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

        public void SaveChanges()
        {
            if (!_layouts.Any())
            {
                return;
            }

            var criteria = new NotificationLayoutSearchCriteria
            {
                Names = _layouts.Select(x => x.Name).ToArray(),
                Take = 1000
            };
            var existingNotificationLayout = _layoutSearchService.SearchAsync(criteria).GetAwaiter().GetResult();

            foreach (var layout in _layouts)
            {
                var existLayout = existingNotificationLayout.Results.FirstOrDefault(x => x.Name.EqualsInvariant(layout.Name));
                layout.Id = existLayout?.Id;
            }

            _layoutService.SaveChangesAsync(_layouts).GetAwaiter().GetResult();

            _layouts.Clear();
        }
    }
}
