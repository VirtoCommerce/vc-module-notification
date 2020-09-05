using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;

namespace VirtoCommerce.NotificationsModule.Data.Senders
{
    public class NotificationMessageSenderFactory : INotificationMessageSenderFactory
    {
        private readonly IEnumerable<INotificationMessageSender> _senders;

        public NotificationMessageSenderFactory(IEnumerable<INotificationMessageSender> senders)
        {
            _senders = senders;
        }

        public INotificationMessageSender GetSender(NotificationMessage message)
        {
           return _senders.FirstOrDefault(x => x.CanSend(message)) ?? throw new OperationCanceledException($"sender not found for {message.Kind}");
        }
    }
}
