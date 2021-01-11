using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Extensions;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsSampleModule.Web.Types;

namespace VirtoCommerce.NotificationsSampleModule.Web.Services
{
    public class SampleService
    {
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;

        public SampleService(INotificationSender notificationSender, INotificationSearchService notificationSearchService)
        {
            _notificationSender = notificationSender;
            _notificationSearchService = notificationSearchService;
        }

        public async Task Do()
        {
            var notification = await _notificationSearchService.GetNotificationAsync<ExtendedSampleEmailNotification>();
            if (notification != null)
            {
                notification.LanguageCode = "en-US";
                notification.SetFromToMembers("from@test.com", "to@test.com");
                await _notificationSender.SendNotificationAsync(notification);
            }
            
        }
    }
}
