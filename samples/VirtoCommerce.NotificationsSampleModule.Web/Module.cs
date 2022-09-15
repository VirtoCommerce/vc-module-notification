using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem;
using VirtoCommerce.NotificationsSampleModule.Web.Filters;
using VirtoCommerce.NotificationsSampleModule.Web.Models;
using VirtoCommerce.NotificationsSampleModule.Web.Repositories;
using VirtoCommerce.NotificationsSampleModule.Web.Services;
using VirtoCommerce.NotificationsSampleModule.Web.Types;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;

namespace VirtoCommerce.NotificationsSampleModule.Web
{
    public class Module : IModule
    {
        public ManifestModuleInfo ModuleInfo { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<TwitterNotificationDbContext>((provider, options) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                options.UseSqlServer(configuration.GetConnectionString(ModuleInfo.Id) ?? configuration.GetConnectionString("VirtoCommerce"));
            });

            serviceCollection.AddTransient<INotificationRepository, TwitterNotificationRepository>();
            serviceCollection.AddTransient<Func<INotificationRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<INotificationRepository>());

            //Register global folders for search templates
            var moduleTemplatesPath = Path.Combine(ModuleInfo.FullPhysicalPath, "Templates");
            serviceCollection.AddFileSystemTemplateLoader(opt =>
            {
                opt.DiscoveryPath = moduleTemplatesPath;
                opt.FallbackDiscoveryPath = Path.Combine(moduleTemplatesPath, "Default");
            });
            //To register a new custom liquid  filter  use the flowing syntax
            serviceCollection.Configure<LiquidRenderOptions>(opt => opt.CustomFilterTypes.Add(typeof(CustomLiquidFilters)));
            //or this alternative syntax
            serviceCollection.AddLiquidRenderer().AddCustomLiquidFilterType(typeof(CustomLiquidFilters));

            serviceCollection.AddTransient<SampleService>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            AbstractTypeFactory<NotificationEntity>.RegisterType<TwitterNotificationEntity>();
            var registrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
            registrar.RegisterNotification<PostTwitterNotification>();
            registrar.RegisterNotification<CustomerInvitationNotification>();

            registrar.RegisterNotification<SampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
            {
                Sample = "",
                Subject = "SampleEmailNotification test",
                Body = "SampleEmailNotification body test",
            });

            var assembly = Assembly.GetExecutingAssembly();
            registrar.OverrideNotificationType<SampleEmailNotification, ExtendedSampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
            {
                Sample = "",
                Subject = assembly.GetManifestResourceStream("VirtoCommerce.NotificationsSampleModule.Web.TemplatesEmbedded.SampleEmailNotification_subject.txt").ReadToString(),
                Body = assembly.GetManifestResourceStream("VirtoCommerce.NotificationsSampleModule.Web.TemplatesEmbedded.SampleEmailNotification_body.html").ReadToString()
            });

            var moduleTemplatesPath = Path.Combine(ModuleInfo.FullPhysicalPath, "Templates");
            //Set individual discovery folder for templates 
            registrar.OverrideNotificationType<RegistrationEmailNotification, NewRegistrationEmailNotification>()
                     .WithTemplatesFromPath(Path.Combine(moduleTemplatesPath, "Custom"), Path.Combine(moduleTemplatesPath, "Default"));

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                using (var notificationDbContext = serviceScope.ServiceProvider.GetRequiredService<TwitterNotificationDbContext>())
                {
                    notificationDbContext.Database.EnsureCreated();
                    notificationDbContext.Database.Migrate();
                }
            }

            //sample for showing how to send a notification
            var service = appBuilder.ApplicationServices.GetService<SampleService>();
            service.Do().GetAwaiter().GetResult();
        }

        public void Uninstall()
        {
        }
    }
}
