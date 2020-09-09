using System;
using System.Collections.Generic;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LiquidRenderOptions
    {
        public HashSet<Type> CustomFilterTypes { get; set; } = new HashSet<Type>();
    }
}
