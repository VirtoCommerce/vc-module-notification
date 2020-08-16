namespace VirtoCommerce.NotificationsModule.Core.Model
{
    public class SmsGatewayOptions
    {
        /// <summary>
        /// Sms gateway account Id 
        /// </summary>
        public string AccountId { get; set; }
        /// <summary>
        /// Sms gateway account password or auth token
        /// </summary>
        public string AccountPassword { get; set; }
    }
}
