using System;
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
        private readonly INotificationLayoutRegistrar _layoutRegistrar;

        public NotificationLayoutsController(
            INotificationLayoutService layoutService,
            INotificationLayoutSearchService layoutSearchService,
            INotificationLayoutRegistrar layoutRegistrar)
        {
            _layoutService = layoutService;
            _layoutSearchService = layoutSearchService;
            _layoutRegistrar = layoutRegistrar;
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public async Task<ActionResult<NotificationLayout>> GetNotificationLayoutById(string id)
        {
            var layout = await _layoutService.GetNoCloneAsync(id);

            if (layout == null)
            {
                // Fallback: treat id as predefined layout name
                layout = _layoutRegistrar.GetByName(id);
                if (layout != null)
                {
                    layout = (NotificationLayout)layout.Clone();
                    layout.Id = id;
                    layout.IsPredefined = true;
                }
            }
            else
            {
                layout = (NotificationLayout)layout.Clone();
                layout.IsPredefined = _layoutRegistrar.GetByName(layout.Name) != null;
            }

            return Ok(layout);
        }

        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<NotificationLayoutSearchResult>> SearchNotificationLayouts([FromBody] NotificationLayoutSearchCriteria searchCriteria)
        {
            var searchResult = await _layoutSearchService.SearchAsync(searchCriteria);

            // Merge predefined layouts that are not overridden by a DB record
            var dbNames = searchResult.Results.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            // Mark DB layouts that have a predefined counterpart
            foreach (var dbLayout in searchResult.Results)
            {
                dbLayout.IsPredefined = _layoutRegistrar.GetByName(dbLayout.Name) != null;
            }

            var predefinedToAdd = _layoutRegistrar.AllRegisteredLayouts
                .Where(x => !dbNames.Contains(x.Name))
                .Select(x =>
                {
                    var clone = (NotificationLayout)x.Clone();
                    clone.Id = x.Name;
                    clone.IsPredefined = true;
                    return clone;
                })
                .ToList();

            if (predefinedToAdd.Count > 0)
            {
                searchResult.Results.AddRange(predefinedToAdd);
                searchResult.TotalCount += predefinedToAdd.Count;
            }

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
        [ProducesResponseType(typeof(NotificationLayout), StatusCodes.Status200OK)]
        public async Task<ActionResult<NotificationLayout>> UpdateNotificationLayout([FromBody] NotificationLayout layout)
        {
            // Predefined layouts are served from memory and never stored in the DB.
            // The GET endpoint uses Name as a synthetic Id for in-memory layouts (id == name convention,
            // see docs/tech-doc.md "Layout Name as Identity"). When the UI saves such a layout for the
            // first time we must assign a real DB UUID so subsequent reads return id != name, which is
            // how the UI distinguishes a DB override from an unmodified predefined layout.
            if (layout.Id == layout.Name && _layoutRegistrar.GetByName(layout.Name) != null)
            {
                // Check whether a DB record already exists for this name (e.g. from a previous save).
                var existing = (await _layoutSearchService.SearchNoCloneAsync(
                    new NotificationLayoutSearchCriteria { Names = new[] { layout.Name }, Take = 1 }))
                    .Results.FirstOrDefault();

                if (existing != null)
                {
                    // Reuse the existing UUID so this becomes an UPDATE, not an INSERT.
                    layout.Id = existing.Id;
                }
                else
                {
                    // No DB record yet — let the service generate a fresh UUID.
                    layout.Id = null;
                }
            }

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
            return Ok(layout);
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

        /// <summary>
        /// Resets a customized layout back to its predefined (in-code) version
        /// by deleting the DB override. The predefined layout is served automatically afterward.
        /// </summary>
        [HttpDelete]
        [Route("{id}/reset")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ResetNotificationLayoutToDefault(string id)
        {
            var dbLayout = await _layoutService.GetNoCloneAsync(id);
            if (dbLayout == null || _layoutRegistrar.GetByName(dbLayout.Name) == null)
            {
                return NotFound();
            }

            await _layoutService.DeleteAsync(new[] { id });

            return NoContent();
        }
    }
}
