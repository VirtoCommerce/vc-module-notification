using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem
{
    public class FileSystemNotificationTemplateLoader : INotificationTemplateLoader
    {
        private readonly FileSystemTemplateLoaderOptions _options;
        public FileSystemNotificationTemplateLoader(IOptions<FileSystemTemplateLoaderOptions> options)
        {
            _options = options.Value;
        }

        /// <summary>
        /// Load templates from 'DiscoveryPath' of Options
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public virtual IEnumerable<NotificationTemplate> LoadTemplates(Notification notification)
        {
            var result = Enumerable.Empty<NotificationTemplate>();
            if (!string.IsNullOrEmpty(_options.DiscoveryPath))
            {
                result = LoadTemplates(notification, _options.DiscoveryPath, _options.FallbackDiscoveryPath);
            }
            return result;
        }

        /// <summary>
        /// Load templates from path
        /// There is default path for loading default templates from defaultPath. It could be null.
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="path"></param>
        /// <param name="defaultPath"></param>
        /// <returns></returns>
        public virtual IEnumerable<NotificationTemplate> LoadTemplates(Notification notification, string path, string defaultPath = null)
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
            if (!string.IsNullOrEmpty(defaultPath))
            {
                var defaultContents = LoadAllNotificationContentsFromPath(notification, defaultPath);
                contents = defaultContents.Concat(contents);
            }
            foreach (var groupMatch in contents.GroupBy(x => x.LanguageCode))
            {
                var template = AbstractTypeFactory<NotificationTemplate>.TryCreateInstance($"{notification.Kind}Template");
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
