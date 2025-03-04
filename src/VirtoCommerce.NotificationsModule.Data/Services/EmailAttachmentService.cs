using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Services;

public class EmailAttachmentService(IHttpClientFactory httpClientFactory) : IEmailAttachmentService
{
    public async Task<Stream> GetStreamAsync(EmailAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        if (!string.IsNullOrEmpty(attachment.Url) &&
            Uri.TryCreate(attachment.Url, UriKind.Absolute, out var uri) &&
            IsHttpScheme(uri.Scheme))
        {
            // Absolute Http URL: Download file from uri
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        // Local file: Open file stream
        var filePath = attachment.Url.EmptyToNull() ?? attachment.FileName;
        return File.OpenRead(filePath);
    }

    private static bool IsHttpScheme(string scheme)
    {
        return scheme.EqualsIgnoreCase("https") ||
               scheme.EqualsIgnoreCase("http");
    }
}
