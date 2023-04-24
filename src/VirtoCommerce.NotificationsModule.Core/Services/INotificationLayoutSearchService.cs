using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.NotificationsModule.Core.Services
{
    /// <summary>
    /// The service is for to make a query to Database and get a list of notification layouts
    /// </summary>
    public interface INotificationLayoutSearchService : ISearchService<NotificationLayoutSearchCriteria, NotificationLayoutSearchResult, NotificationLayout>
    {
    }
}
