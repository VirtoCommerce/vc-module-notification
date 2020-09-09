using System.ComponentModel.DataAnnotations;

namespace VirtoCommerce.NotificationsModule.Twilio
{
    public class TwilioSenderOptions
    {
        /// <summary>
        /// Sms gateway account Id
        /// </summary>
        [Required]
        public string AccountId { get; set; }

        /// <summary>
        /// Sms gateway account password or auth token
        /// </summary>
        [Required]
        public string AccountPassword { get; set; }
    }
}
