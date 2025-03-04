using System;
using System.IO;
using System.Threading.Tasks;

namespace VirtoCommerce.NotificationsModule.Core.Extensions;

public static class StreamExtensions
{
    public static async Task<byte[]> ReadAllBytesAsync(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}
