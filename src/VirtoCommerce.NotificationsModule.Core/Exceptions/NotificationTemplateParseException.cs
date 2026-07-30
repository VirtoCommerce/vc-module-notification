using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Exceptions;

namespace VirtoCommerce.NotificationsModule.Core.Exceptions
{
    /// <summary>
    /// Thrown when a notification template cannot be parsed. The template text is supplied by the
    /// caller, so API endpoints should surface this as invalid input rather than a server fault.
    /// </summary>
    public class NotificationTemplateParseException : PlatformException
    {
        public NotificationTemplateParseException(IEnumerable<string> errors)
            : base(string.Join("\r\n", errors))
        {
            Errors = errors.ToArray();
        }

        public string[] Errors { get; }
    }
}
