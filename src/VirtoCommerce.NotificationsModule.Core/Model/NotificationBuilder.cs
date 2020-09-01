using System;
using System.Collections.Generic;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public class NotificationBuilder
    {
        public IList<NotificationTemplate> PredefinedTemplates { get; private set; } = new List<NotificationTemplate>();
        public string DiscoveryPath { get; private set; }
        public string FallbackDiscoveryPath { get; private set; }

        public NotificationBuilder WithTemplatesFromPath(string path, string fallbackPath = null)
        {
            DiscoveryPath = path;
            FallbackDiscoveryPath = fallbackPath;
            return this;
        }

        public NotificationBuilder WithTemplates(params NotificationTemplate[] templates)
        {
            if (templates == null)
            {
                throw new ArgumentNullException(nameof(templates));
            }
            foreach (var template in templates)
            {
                template.IsReadonly = true;
                template.IsPredefined = true;
                PredefinedTemplates.Add(template);
            }
            return this;
        }
    }
}
