using System.Text.RegularExpressions;

namespace VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem
{
    public class FileSystemTemplateLoaderOptions
    {
        //The file system folder that will be used to discovery the notification templates for all registered templates
        public string DiscoveryPath { get; set; }
        //The file system folder that is used to discover alternative notification templates.
        //All found templates will only complement the templates are found on the main folder located by DiscoveryPath
        public string FallbackDiscoveryPath { get; set; }
        // The regex pattern used to identify notification template files
        // It can be configured by standard appsettings.json file
        public string TemplateFilePattern { get; set; } = @"(?<type>[\w-]+)_(?<part>body|subject|sample)(?<lang>\.[a-z]{1,8}(-[A-Za-z0-9]{1,8})?)?\.[\w]+$";

        public Regex TemplateFileRegex => new(TemplateFilePattern, RegexOptions.IgnoreCase);
    }
}
