using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.NotificationsModule.Data.BackgroundJobs;
using VirtoCommerce.NotificationsModule.Data.Handlers;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.Events;
using Xunit;

namespace VirtoCommerce.NotificationsModule.Tests.UnitTests
{
    // BackgroundJob.Initialize sets process-wide static state; share a collection with every other test class
    // that initializes it so they never run in parallel.
    [Collection(BackgroundJobFacadeCollection.Name)]
    public class RequestPasswordResetHandlerUnitTests
    {
        private readonly Mock<IBackgroundJob> _backgroundJob = new();
        private readonly RequestPasswordResetHandler _handler = new();
        private readonly UserRequestPasswordResetEvent _event = new(new ApplicationUser { Email = "user@example.com" }, "https://example.com/reset");

        public RequestPasswordResetHandlerUnitTests()
        {
            _backgroundJob
                .Setup(x => x.Enqueue<SendResetPasswordNotificationJob>(It.IsAny<object>(), It.IsAny<EnqueueOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("job-id");
            BackgroundJob.Initialize(new ServiceCollection().AddScoped(_ => _backgroundJob.Object).BuildServiceProvider());
        }

        [Fact]
        public async Task Handle_EnqueuesResetPasswordJobWithEvent()
        {
            await _handler.Handle(_event);

            VerifyEnqueuedOnce();
        }

#pragma warning disable VC0015 // the legacy entry point is exactly what this test covers
        [Fact]
        public async Task TryToSendNotificationsAsync_LegacyJob_HandsOffToResetPasswordJob()
        {
            await _handler.TryToSendNotificationsAsync(_event);

            VerifyEnqueuedOnce();
        }
#pragma warning restore VC0015

        private void VerifyEnqueuedOnce()
        {
            _backgroundJob.Verify(x => x.Enqueue<SendResetPasswordNotificationJob>(
                It.Is<object>(p => ((SendResetPasswordNotificationPayload)p).Event == _event),
                It.IsAny<EnqueueOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [CollectionDefinition(Name, DisableParallelization = true)]
    public class BackgroundJobFacadeCollection
    {
        public const string Name = "BackgroundJob static facade";
    }
}
