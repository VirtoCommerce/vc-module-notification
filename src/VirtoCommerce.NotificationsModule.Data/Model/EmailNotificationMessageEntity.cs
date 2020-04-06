using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Model
{
    public class EmailNotificationMessageEntity : NotificationMessageEntity
    {
        [NotMapped]
        public override string Kind => nameof(EmailNotification);

        /// <summary>
        /// Subject of message
        /// </summary>
        [StringLength(512)]
        public string Subject { get; set; }

        /// <summary>
        /// Body of message
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Sender info (e-mail) of message
        /// </summary>
        [StringLength(128)]
        public string From { get; set; }

        /// <summary>
        /// Recipient info (e-mail) of message
        /// </summary>
        [StringLength(128)]
        public string To { get; set; }

        [StringLength(1024)]
        public string CC { get; set; }

        [StringLength(1024)]
        public string BCC { get; set; }

        public override NotificationMessage ToModel(NotificationMessage message)
        {
            if (message is EmailNotificationMessage emailNotificationMessage)
            {
                emailNotificationMessage.Subject = Subject;
                emailNotificationMessage.Body = Body;
                emailNotificationMessage.From = From;
                emailNotificationMessage.To = To;
                emailNotificationMessage.CC = CC?.Split(";");
                emailNotificationMessage.BCC = BCC?.Split(";");
            }

            return base.ToModel(message);
        }

        public override NotificationMessageEntity FromModel(NotificationMessage message, PrimaryKeyResolvingMap pkMap)
        {
            if (message is EmailNotificationMessage emailNotificationMessage)
            {
                Subject = emailNotificationMessage.Subject;
                Body = emailNotificationMessage.Body;
                From = emailNotificationMessage.From;
                To = emailNotificationMessage.To;

                if (!emailNotificationMessage.CC.IsNullOrEmpty())
                {
                    CC = string.Join(";", emailNotificationMessage.CC);
                }

                if (!emailNotificationMessage.BCC.IsNullOrEmpty())
                {
                    BCC = string.Join(";", emailNotificationMessage.BCC);
                }
            }

            return base.FromModel(message, pkMap);
        }

        public override void Patch(NotificationMessageEntity message)
        {
            if (message is EmailNotificationMessageEntity emailNotificationMessageEntity)
            {
                emailNotificationMessageEntity.Subject = Subject;
                emailNotificationMessageEntity.Body = Body;
                emailNotificationMessageEntity.From = From;
                emailNotificationMessageEntity.To = To;
                emailNotificationMessageEntity.CC = CC;
                emailNotificationMessageEntity.BCC = BCC;
            }

            base.Patch(message);
        }
    }
}
