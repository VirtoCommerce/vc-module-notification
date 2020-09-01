using System;

namespace VirtoCommerce.NotificationsModule.Core.Model
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class NotificationParameterAttribute : Attribute
    {
        public NotificationParameterAttribute(string alias)
        {
            Alias = alias;
        }

        public string Alias { get; set; }
    }
}
