using System;
using System.Collections.Generic;
using VirtoCommerce.NotificationsModule.Core;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer
{
    public class LiquidRenderOptions
    {
        public HashSet<Type> CustomFilterTypes { get; set; } = new HashSet<Type>();

        public int LoopCount { get; set; } = ModuleConstants.DefaultLiquidRendererLoopCount;
    }
}
