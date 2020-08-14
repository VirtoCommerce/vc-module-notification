using System.Text.RegularExpressions;

namespace VirtoCommerce.NotificationsModule.Data.TemplateLoaders
{
    public class FileSystemTemplateLoaderOptions
    {
        public Regex TemplateFilePattern { get; set; } = new Regex(@"(?<type>[\w-]+)_(?<part>body|subject)(?<lang>\.[a-z]{2}(-[A-Z]{2})?)?\.[\w]+$", RegexOptions.IgnoreCase);
    }
}
