using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.NotificationsModule.Core
{
    [ExcludeFromCodeCoverage]
    public static class ModuleConstants
    {
        public static int DefaultLiquidRendererLoopLimit { get; } = 2000;

        /// <summary>
        /// Name (and id) of the predefined Master layout. Modules opt a template in with .WithLayout(DefaultLayoutName).
        /// </summary>
        public const string DefaultLayoutName = "Default";

        public static class Security
        {
            public static class Permissions
            {
                public const string Read = "notifications:read";
                public const string Create = "notifications:create";
                public const string Access = "notifications:access";
                public const string Update = "notifications:update";
                public const string Delete = "notifications:delete";
                public const string Export = "notifications:export";
                public const string Import = "notifications:import";
                public const string ReadTemplates = "notifications:templates:read";
                public const string CreateTemplate = "notifications:template:create";

                public static string[] AllPermissions = new[] { Read, Create, Access, Update, Delete, Export, Import, ReadTemplates, CreateTemplate };
            }
        }

        public static class Settings
        {
            public static class General
            {
                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield break;
                    }
                }
            }

            /// <summary>
            /// Brand of the emails rendered in the Default Master layout. Registered globally and for stores;
            /// a value set on the store wins over the global one.
            /// </summary>
            public static class Brand
            {
                private const string GroupName = "Notifications|Email brand";

                public static SettingDescriptor LogoText { get; } = new()
                {
                    Name = "Notifications.Brand.LogoText",
                    GroupName = GroupName,
                    ValueType = SettingValueType.ShortText,
                };

                public static SettingDescriptor LogoUrl { get; } = new()
                {
                    Name = "Notifications.Brand.LogoUrl",
                    GroupName = GroupName,
                    ValueType = SettingValueType.ShortText,
                };

                public static SettingDescriptor LogoWidth { get; } = new()
                {
                    Name = "Notifications.Brand.LogoWidth",
                    GroupName = GroupName,
                    ValueType = SettingValueType.PositiveInteger,
                    DefaultValue = 160,
                };

                public static SettingDescriptor AccentColor { get; } = new()
                {
                    Name = "Notifications.Brand.AccentColor",
                    GroupName = GroupName,
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "#e8912a",
                };

                public static SettingDescriptor HeaderStyle { get; } = new()
                {
                    Name = "Notifications.Brand.HeaderStyle",
                    GroupName = GroupName,
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "line",
                    AllowedValues = ["line", "dark", "accent"],
                };

                public static SettingDescriptor EmailWidth { get; } = new()
                {
                    Name = "Notifications.Brand.EmailWidth",
                    GroupName = GroupName,
                    ValueType = SettingValueType.PositiveInteger,
                    DefaultValue = 600,
                };

                public static SettingDescriptor FooterText { get; } = new()
                {
                    Name = "Notifications.Brand.FooterText",
                    GroupName = GroupName,
                    ValueType = SettingValueType.LongText,
                };

                public static SettingDescriptor Address { get; } = new()
                {
                    Name = "Notifications.Brand.Address",
                    GroupName = GroupName,
                    ValueType = SettingValueType.LongText,
                };

                public static SettingDescriptor SocialLinks { get; } = new()
                {
                    Name = "Notifications.Brand.SocialLinks",
                    GroupName = GroupName,
                    ValueType = SettingValueType.Json,
                };

                public static SettingDescriptor UtilityLinks { get; } = new()
                {
                    Name = "Notifications.Brand.UtilityLinks",
                    GroupName = GroupName,
                    ValueType = SettingValueType.Json,
                };

                public static SettingDescriptor ShowUnsubscribe { get; } = new()
                {
                    Name = "Notifications.Brand.ShowUnsubscribe",
                    GroupName = GroupName,
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false,
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return LogoText;
                        yield return LogoUrl;
                        yield return LogoWidth;
                        yield return AccentColor;
                        yield return HeaderStyle;
                        yield return EmailWidth;
                        yield return FooterText;
                        yield return Address;
                        yield return SocialLinks;
                        yield return UtilityLinks;
                        yield return ShowUnsubscribe;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings
            {
                get
                {
                    return General.AllSettings.Concat(Brand.AllSettings);
                }
            }

            public static IEnumerable<SettingDescriptor> StoreLevelSettings
            {
                get
                {
                    return Brand.AllSettings;
                }
            }
        }

    }
}
