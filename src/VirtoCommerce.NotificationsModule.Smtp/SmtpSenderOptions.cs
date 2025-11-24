using System;
using System.Collections.Generic;

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
        /// Force SSL/TLS
        /// <remarks>
        /// Don't need to be enabled if server supports STARTTLS
        /// </remarks>
        /// </summary>
        public bool ForceSslTls { get; set; }

        /// <summary>
        /// Custom headers for email messages
        /// </summary>
        public IDictionary<string, string> CustomHeaders { get; set; }

        /// <summary>
        /// Force SSL/TLS
        /// <remarks>
        /// Don't need to be enabled if server supports STARTTLS
        /// </remarks>
        /// </summary>
        [Obsolete("Use ForceSslTls instead")]
        public bool EnableSsl
        {
            get => ForceSslTls;
            set => ForceSslTls = value;
        }
    }
}
