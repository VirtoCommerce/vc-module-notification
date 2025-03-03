using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests;
public class EmailAttachmentServiceUnitTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly EmailAttachmentService _emailAttachmentService;

    public EmailAttachmentServiceUnitTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _emailAttachmentService = new EmailAttachmentService(_httpClientFactoryMock.Object);
    }

    [Fact]
    public async Task GetStreamAsync_LocalFile_ReturnsFileStream()
    {
        // Arrange
        var fileName = Path.GetTempFileName();
        var attachment = new EmailAttachment { FileName = fileName };
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(attachment.FileName, expectedContent);

        try
        {
            // Act
            using var stream = await _emailAttachmentService.GetStreamAsync(attachment);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(expectedContent, content);
        }
        finally
        {
            // Cleanup
            File.Delete(attachment.FileName);
        }
    }

    [Fact]
    public async Task GetStreamAsync_Url_ReturnsStreamFromUrl()
    {
        // Arrange
        var attachment = new EmailAttachment { Url = "http://example.com/test.txt" };
        var expectedContent = "Test content";
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedContent)
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        using var stream = await _emailAttachmentService.GetStreamAsync(attachment);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public async Task ReadAllBytesAsync_LocalFile_ReturnsFileBytes()
    {
        // Arrange
        var fileName = Path.GetTempFileName();
        var attachment = new EmailAttachment { FileName = fileName };
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(attachment.FileName, expectedContent);

        try
        {
            // Act
            var bytes = await _emailAttachmentService.ReadAllBytesAsync(attachment);
            var content = System.Text.Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.Equal(expectedContent, content);
        }
        finally
        {
            // Cleanup
            File.Delete(attachment.FileName);
        }
    }

    [Fact]
    public async Task ReadAllBytesAsync_Url_ReturnsBytesFromUrl()
    {
        // Arrange
        var attachment = new EmailAttachment { Url = "http://example.com/test.txt" };
        var expectedContent = "Test content";
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedContent)
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        var bytes = await _emailAttachmentService.ReadAllBytesAsync(attachment);
        var content = System.Text.Encoding.UTF8.GetString(bytes);

        // Assert
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public async Task GetStreamAsync_RelativeUrl_ReturnsFileStream()
    {
        // Arrange
        var fileName = Path.GetFileName(Path.GetTempFileName());
        var attachment = new EmailAttachment { Url = fileName };
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(attachment.Url, expectedContent);

        try
        {
            // Act
            using var stream = await _emailAttachmentService.GetStreamAsync(attachment);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal(expectedContent, content);
        }
        finally
        {
            // Cleanup
            File.Delete(attachment.Url);
        }
    }

    [Fact]
    public async Task ReadAllBytesAsync_RelativeUrl_ReturnsFileBytes()
    {
        // Arrange
        var fileName = Path.GetTempFileName();
        var attachment = new EmailAttachment { Url = fileName };
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(attachment.Url, expectedContent);

        try
        {
            // Act
            var bytes = await _emailAttachmentService.ReadAllBytesAsync(attachment);
            var content = System.Text.Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.Equal(expectedContent, content);
        }
        finally
        {
            // Cleanup
            File.Delete(attachment.Url);

        }
    }
}
