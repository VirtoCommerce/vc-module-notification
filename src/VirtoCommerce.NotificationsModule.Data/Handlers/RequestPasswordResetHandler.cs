using System;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Data.BackgroundJobs;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.Platform.Core.Security.Events;

namespace VirtoCommerce.NotificationsModule.Data.Handlers
{
    public class RequestPasswordResetHandler : IEventHandler<UserRequestPasswordResetEvent>
    {
        public Task Handle(UserRequestPasswordResetEvent message)
        {
            return BackgroundJob.Enqueue<SendResetPasswordNotificationJob>(
                new SendResetPasswordNotificationPayload { Event = message });
        }

        /// <summary>
        /// Kept for background jobs enqueued by an earlier version, which reference this method by name.
        /// Hands the work to <see cref="SendResetPasswordNotificationJob"/>; remove this once no such job
        /// can still be pending.
        /// </summary>
        // Signature is byte-identical on purpose: Hangfire persists a queued job as type name + method name +
        // parameter types + serialized args, so changing any of them would strand already-queued entries as Failed.
        [Obsolete("Enqueued indirectly by legacy Hangfire jobs only; new work uses SendResetPasswordNotificationJob.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public Task TryToSendNotificationsAsync(UserRequestPasswordResetEvent argument)
        {
            return BackgroundJob.Enqueue<SendResetPasswordNotificationJob>(
                new SendResetPasswordNotificationPayload { Event = argument });
        }
    }
}
