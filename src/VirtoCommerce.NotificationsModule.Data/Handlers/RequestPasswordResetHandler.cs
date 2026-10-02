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
    }
}
