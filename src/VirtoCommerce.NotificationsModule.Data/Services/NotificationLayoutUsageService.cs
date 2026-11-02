using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.NotificationsModule.Data.Services;

public class NotificationLayoutUsageService : INotificationLayoutUsageService
{
    private readonly Func<INotificationRepository> _repositoryFactory;
    private readonly INotificationLayoutService _layoutService;
    private readonly INotificationRegistrar _notificationRegistrar;

    public NotificationLayoutUsageService(
        Func<INotificationRepository> repositoryFactory,
        INotificationLayoutService layoutService,
        INotificationRegistrar notificationRegistrar)
    {
        _repositoryFactory = repositoryFactory;
        _layoutService = layoutService;
        _notificationRegistrar = notificationRegistrar;
    }

    public virtual async Task<NotificationLayoutUsage> GetUsageAsync(string layoutId)
    {
        var layout = await _layoutService.GetNoCloneAsync(layoutId);
        if (layout == null)
        {
            return null;
        }

        // A template can point to a predefined layout by its name or to its DB override by id
        var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { layout.Id };
        if (layout.IsPredefined)
        {
            references.Add(layout.Name);
        }

        bool UsesLayout(string templateLayoutId) =>
            string.IsNullOrEmpty(templateLayoutId) ? layout.IsDefault : references.Contains(templateLayoutId);

        var dbTemplates = await GetDbTemplatesAsync();

        // A global DB template replaces the predefined one for the same type and language
        var overridden = dbTemplates
            .Where(x => string.IsNullOrEmpty(x.TenantId))
            .Select(x => (x.Type, Language: x.LanguageCode ?? string.Empty))
            .ToHashSet();

        var predefinedTypes = _notificationRegistrar.AllRegisteredNotifications
            .SelectMany(notification => (notification.Templates ?? []).OfType<EmailNotificationTemplate>()
                .Where(template => !overridden.Contains((notification.Type, template.LanguageCode ?? string.Empty)))
                .Where(template => UsesLayout(template.NotificationLayoutId))
                .Select(_ => notification.Type));

        var dbTypes = dbTemplates
            .Where(x => UsesLayout(x.NotificationLayoutId))
            .Select(x => x.Type);

        return new NotificationLayoutUsage
        {
            LayoutId = layout.Id,
            NotificationTypes = predefinedTypes
                .Concat(dbTypes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList(),
        };
    }

    protected virtual async Task<IList<DbTemplateInfo>> GetDbTemplatesAsync()
    {
        using var repository = _repositoryFactory();
        repository.DisableChangesTracking();

        return await repository.Notifications
            .SelectMany(x => x.Templates.OfType<EmailNotificationTemplateEntity>(), (notification, template) => new DbTemplateInfo
            {
                Type = notification.Type,
                TenantId = notification.TenantId,
                LanguageCode = template.LanguageCode,
                NotificationLayoutId = template.NotificationLayoutId,
            })
            .ToListAsync();
    }

    protected class DbTemplateInfo
    {
        public string Type { get; set; }
        public string TenantId { get; set; }
        public string LanguageCode { get; set; }
        public string NotificationLayoutId { get; set; }
    }
}
