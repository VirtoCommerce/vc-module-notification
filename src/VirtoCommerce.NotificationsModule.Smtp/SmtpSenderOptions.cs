namespace VirtoCommerce.NotificationsModule.Smtp
{
    /// <summary>
    /// Smtp protocol
    /// </summary>
    public class SmtpSenderOptions
    {
        /// <summary>
        /// Server of Sending
        /// </summary>
        public string SmtpServer { get; set; }

        /// <summary>
        /// Port of Server
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Login of Sending Server
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Password of Sending Server
        /// <remarks>
        /// For GMail check https://support.google.com/accounts/answer/185833?hl=en
        /// because now it's impossible to use GMail SMTP without 2-step verification
        /// </remarks>
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Enable SSL option
        /// <remarks>
        /// Don't need to be enabled if server supports STARTTLS
        /// </remarks>
        /// </summary>
        public bool EnableSsl { get; set; }
    }
}
