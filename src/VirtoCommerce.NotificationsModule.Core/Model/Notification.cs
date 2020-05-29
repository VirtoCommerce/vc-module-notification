using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    /// <summary>
    /// Base class for Notification
    /// </summary>
    public abstract class Notification : AuditableEntity, ICloneable, IHasOuterId
    {
        [Obsolete("need to use ctor with 'type' parameter")]
        public Notification()
        {
            Type = GetType().Name;
        }

        protected Notification(string type)
        {
            Type = type;
        }

        /// <summary>
        /// For detecting owner
        /// </summary>
        public TenantIdentity TenantIdentity { get; set; } = TenantIdentity.Empty;
        public bool IsActive { get; set; }
        public string LanguageCode { get; set; }

        /// <summary>
        /// This field represents an alias for the notification type
        /// and is used only for backward compatibility with old notification names
        /// that are stored and used by API clients.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Type of notifications, like Identifier
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// For detecting kind of notifications (email, sms and etc.)
        /// </summary>
        public abstract string Kind { get; }
        public string OuterId { get; set; }
        public IList<NotificationTemplate> Templates { get; set; }

        public virtual Task ToMessageAsync(NotificationMessage message, INotificationTemplateRenderer render)
        {
            message.TenantIdentity = new TenantIdentity(TenantIdentity?.Id, TenantIdentity?.Type);
            message.NotificationType = Type;
            message.NotificationId = Id;
            message.LanguageCode = LanguageCode;

            return Task.CompletedTask;
        }

        public virtual void ReduceDetails(string responseGroup)
        {
            //Reduce details according to response group
            var notificationResponseGroup = EnumUtility.SafeParseFlags(responseGroup, NotificationResponseGroup.Full);

            if (!notificationResponseGroup.HasFlag(NotificationResponseGroup.WithTemplates))
            {
                Templates = null;
            }
        }

        public abstract void SetFromToMembers(string from, string to);

        public virtual Notification PopulateFromRequest(Notification request)
        {
            LanguageCode = request.LanguageCode;

            return this;
        }

        #region ICloneable members

        public virtual object Clone()
        {
            var result = MemberwiseClone() as Notification;

            result.Templates = Templates?.Select(x => x.Clone()).OfType<NotificationTemplate>().ToList();
            result.TenantIdentity = TenantIdentity?.Clone() as TenantIdentity;

            return result;
        }

        #endregion
    }
}
