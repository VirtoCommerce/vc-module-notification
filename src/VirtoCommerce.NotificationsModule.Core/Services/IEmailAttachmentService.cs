using System.IO;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services;

public interface IEmailAttachmentService
{
    Task<Stream> GetStreamAsync(EmailAttachment attachment);
}

