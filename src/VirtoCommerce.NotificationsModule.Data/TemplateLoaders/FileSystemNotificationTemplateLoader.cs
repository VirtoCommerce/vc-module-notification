using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.TemplateLoaders
{
    public class FileSystemNotificationTemplateLoader : INotificationTemplateLoader
    {
        private readonly FileSystemTemplateLoaderOptions _options;
        public FileSystemNotificationTemplateLoader(IOptions<FileSystemTemplateLoaderOptions> options)
        {
            _options = options.Value;
        }

        public virtual IEnumerable<NotificationTemplate> LoadTemplates(Notification notification, string path, string fallbackPath = null)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(path);
            }

            var result = new List<NotificationTemplate>();

            var contents = LoadAllNotificationContentsFromPath(notification, path);
            if (!string.IsNullOrEmpty(fallbackPath))
            {
                var fallbackContents = LoadAllNotificationContentsFromPath(notification, fallbackPath);
                contents = fallbackContents.Concat(contents);
            }
            foreach (var groupMatch in contents.GroupBy(x => x.LanguageCode))
            {
                var template = AbstractTypeFactory<NotificationTemplate>.TryCreateInstance($"{notification.Kind}Template");
                template.IsReadonly = true;
                template.IsPredefined = true;
                template.LanguageCode = groupMatch.Key;

                groupMatch.Apply(x => template.PopulateFromLocalizedContent(x));

                result.Add(template);
            }

            return result;
        }

        protected virtual IEnumerable<LocalizedTemplateContent> LoadAllNotificationContentsFromPath(Notification notification, string path)
        {
            var result = new HashSet<LocalizedTemplateContent>();
            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path, $"{notification.Type}*.*", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    var match = _options.TemplateFilePattern.Match(file);
                    if (match.Success)
                    {
                        result.Add(new LocalizedTemplateContent
                        {
                            LanguageCode = match.Groups["lang"]?.Value.Trim().TrimStart('.'),
                            PartName = match.Groups["part"]?.Value,
                            Content = File.ReadAllText(file)
                        });
                    }
                }
            }
            return result;
        }

    }
}
