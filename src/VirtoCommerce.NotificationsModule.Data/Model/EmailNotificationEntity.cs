using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Model
{
    public class EmailNotificationEntity : NotificationEntity
    {
        /// <summary>
        /// Sender info (e-mail, phone number and etc.) of notification
        /// </summary>
        [StringLength(128)]
        public string From { get; set; }

        /// <summary>
        /// Recipient info (e-mail, phone number and etc.) of notification
        /// </summary>
        [StringLength(128)]
        public string To { get; set; }

        #region Navigation Properties

        public virtual ObservableCollection<NotificationEmailRecipientEntity> Recipients { get; set; }
            = new NullCollection<NotificationEmailRecipientEntity>();
        #endregion

        public override Notification ToModel(Notification notification)
        {
            base.ToModel(notification);

            var emailNotification = notification as EmailNotification;

            if (emailNotification != null)
            {
                emailNotification.From = From;
                emailNotification.To = To;
                if (!Recipients.IsNullOrEmpty())
                {
                    emailNotification.CC = Recipients.Where(r => r.RecipientType == NotificationRecipientType.Cc)
                        .Select(cc => cc.EmailAddress).ToArray();
                    emailNotification.BCC = Recipients.Where(r => r.RecipientType == NotificationRecipientType.Bcc)
                        .Select(bcc => bcc.EmailAddress).ToArray();
                }
            }

            return notification;
        }

        public override NotificationEntity FromModel(Notification notification, PrimaryKeyResolvingMap pkMap)
        {
            if (notification is EmailNotification emailNotification)
            {
                From = emailNotification.From;
                To = emailNotification.To;

                if (emailNotification.CC != null)
                {
                    if (Recipients.IsNullCollection()) Recipients = new ObservableCollection<NotificationEmailRecipientEntity>();
                    Recipients.AddRange(emailNotification.CC.Select(cc => AbstractTypeFactory<NotificationEmailRecipientEntity>.TryCreateInstance()
                        .FromModel(cc, NotificationRecipientType.Cc)));
                }

                if (emailNotification.BCC != null)
                {
                    if (Recipients.IsNullCollection()) Recipients = new ObservableCollection<NotificationEmailRecipientEntity>();
                    Recipients.AddRange(emailNotification.BCC.Select(bcc => AbstractTypeFactory<NotificationEmailRecipientEntity>.TryCreateInstance()
                        .FromModel(bcc, NotificationRecipientType.Bcc)));
                }
            }

            return base.FromModel(notification, pkMap);
        }

        public override void Patch(NotificationEntity notification)
        {
            if (notification is EmailNotificationEntity emailNotification)
            {
                emailNotification.From = From;
                emailNotification.To = To;

                if (!Recipients.IsNullCollection())
                {
                    Recipients.Patch(emailNotification.Recipients, (source, target) => source.Patch(target));
                }
            }

            base.Patch(notification);
        }        
    }

    /// <summary>
    /// Type of recipient
    /// </summary>
    public enum NotificationRecipientType
    {
        Cc = 1,
        Bcc
    }
}
