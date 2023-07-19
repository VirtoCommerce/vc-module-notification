using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.NotificationsModule.Core;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Model.Search;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Web.Controllers
{
    [Route("api/notification-layouts")]
    [Authorize]
    public class NotificationLayoutsController : Controller
    {
        private readonly INotificationLayoutService _layoutService;
        private readonly INotificationLayoutSearchService _layoutSearchService;

        public NotificationLayoutsController(
            INotificationLayoutService layoutService,
            INotificationLayoutSearchService layoutSearchService)
        {
            _layoutService = layoutService;
            _layoutSearchService = layoutSearchService;
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<Notification>> GetNotificationLayoutById(string id)
        {
            var layout = await _layoutService.GetNoCloneAsync(id);
            return Ok(layout);
        }

        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<NotificationLayoutSearchResult>> SearchNotificationLayouts([FromBody] NotificationLayoutSearchCriteria searchCriteria)
        {
            var searchResult = await _layoutSearchService.SearchNoCloneAsync(searchCriteria);
            return Ok(searchResult);
        }

        [HttpPost]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Create)]
        public async Task<ActionResult<NotificationLayout>> CreateNotificationLayout([FromBody] NotificationLayout layout)
        {
            await _layoutService.SaveChangesAsync(new[] { layout });
            return Ok(layout);
        }

        [HttpPut]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> UpdateNotificationLayout([FromBody] NotificationLayout layout)
        {
            var layouts = new List<NotificationLayout> { layout };

            if (layout.IsDefault)
            {
                var layoutSearchResult = await _layoutSearchService.SearchAsync(new NotificationLayoutSearchCriteria() { IsDefault = true });
                var defaultLayout = layoutSearchResult.Results.FirstOrDefault();

                if (defaultLayout != null)
                {
                    defaultLayout.IsDefault = false;
                    layouts.Add(defaultLayout);
                }
            }

            await _layoutService.SaveChangesAsync(layouts);
            return NoContent();
        }

        [HttpDelete]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Delete)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteNotificationLayout([FromQuery] string[] ids)
        {
            await _layoutService.DeleteAsync(ids);
            return NoContent();
        }
    }
}
