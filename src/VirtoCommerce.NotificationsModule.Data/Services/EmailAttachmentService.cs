using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Model;

namespace VirtoCommerce.NotificationsModule.Core.Services;

public class EmailAttachmentService : IEmailAttachmentService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailAttachmentService(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _httpClientFactory = httpClientFactory;
    }

    public async Task<Stream> GetStreamAsync(EmailAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        if (!string.IsNullOrEmpty(attachment.Url) &&
            Uri.TryCreate(attachment.Url, UriKind.Absolute, out var uri) &&
            IsHttpScheme(uri.Scheme))
        {
            // Absolute Http URL: Download file from uri
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        // Local file: Open file stream
        var filePath = !string.IsNullOrEmpty(attachment.Url) ? attachment.Url : attachment.FileName;
        return File.OpenRead(filePath);
    }

    private static bool IsHttpScheme(string scheme)
    {
        return scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
               scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
    }
}
