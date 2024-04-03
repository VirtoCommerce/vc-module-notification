using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.NotificationsModule.Data.Model
{
    /// <summary>
    /// Entity is attachment of email
    /// </summary>
    public class EmailAttachmentEntity : AuditableEntity
    {
        /// <summary>
        /// Name of Attachment
        /// </summary>
        [StringLength(1024)]
        public string FileName { get; set; }

        /// <summary>
        /// Url of Attachment
        /// </summary>
        [Required, StringLength(2083)]
        public string Url { get; set; }

        /// <summary>
        /// MimeType of Attachment
        /// </summary>
        [StringLength(128)]
        public string MimeType { get; set; }

        /// <summary>
        /// Size of Attachment
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Language of Attachment
        /// </summary>
        [StringLength(10)]
        public string LanguageCode { get; set; }

        #region Navigation Properties

        /// <summary>
        /// Id of notification message
        /// </summary>
        public string NotificationMessageId { get; set; }
        public EmailNotificationMessageEntity EmailNotificationMessage { get; set; }

        #endregion

        public virtual EmailAttachment ToModel(EmailAttachment emailAttachment)
        {
            ArgumentNullException.ThrowIfNull(emailAttachment);

            emailAttachment.FileName = FileName;
            emailAttachment.Url = Url;
            emailAttachment.MimeType = MimeType;
            emailAttachment.Size = Size;
            emailAttachment.LanguageCode = LanguageCode;

            return emailAttachment;
        }

        public virtual EmailAttachmentEntity FromModel(EmailAttachment emailAttachment)
        {
            ArgumentNullException.ThrowIfNull(emailAttachment);

            FileName = emailAttachment.FileName;
            Url = emailAttachment.Url;
            MimeType = emailAttachment.MimeType;
            Size = emailAttachment.Size;
            LanguageCode = emailAttachment.LanguageCode;

            return this;
        }

        public void Patch(EmailAttachmentEntity attachment)
        {
            attachment.LanguageCode = LanguageCode;
            attachment.FileName = FileName;
            attachment.MimeType = MimeType;
            attachment.Size = Size;
            attachment.Url = Url;
        }
    }
}
