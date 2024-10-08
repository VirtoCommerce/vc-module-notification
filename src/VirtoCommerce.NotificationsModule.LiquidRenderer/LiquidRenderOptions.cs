using System;
using System.Collections.Generic;
using Scriban.Parsing;
using VirtoCommerce.NotificationsModule.Core;

namespace VirtoCommerce.NotificationsModule.LiquidRenderer;

public class LiquidRenderOptions
{
    public HashSet<Type> CustomFilterTypes { get; set; } = new HashSet<Type>();

    public int LoopLimit { get; set; } = ModuleConstants.DefaultLiquidRendererLoopLimit;
    /// <summary>
    /// Defines the language the parser should use for layouts.
    /// </summary>
    public ScriptLang TemplateScriptLanguage { get; set; } = ScriptLang.Liquid;
}
