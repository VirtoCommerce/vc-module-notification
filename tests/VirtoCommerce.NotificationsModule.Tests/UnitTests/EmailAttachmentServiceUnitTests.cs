using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Data.Services;
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
    public async Task GetStreamAsync_LocalFile_ReturnsStream()
    {
        // Arrange
        var fileName = Path.GetTempFileName();
        var attachment = new EmailAttachment { FileName = fileName };
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(attachment.FileName, expectedContent, TestContext.Current.CancellationToken);

        try
        {
            // Act
            await using var stream = await _emailAttachmentService.GetStreamAsync(attachment);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

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
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Act
        await using var stream = await _emailAttachmentService.GetStreamAsync(attachment);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public async Task GetStreamAsync_RelativeUrl_ReturnsStream()
    {
        // Arrange
        var fileName = Path.GetFileName(Path.GetTempFileName());
        var attachment = new EmailAttachment { Url = fileName };
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(attachment.Url, expectedContent, TestContext.Current.CancellationToken);

        try
        {
            // Act
            await using var stream = await _emailAttachmentService.GetStreamAsync(attachment);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

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
