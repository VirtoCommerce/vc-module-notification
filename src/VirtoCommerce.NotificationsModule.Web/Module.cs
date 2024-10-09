using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VirtoCommerce.Notifications.Core.Types;
using VirtoCommerce.NotificationsModule.Core;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.ExportImport;
using VirtoCommerce.NotificationsModule.Data.Handlers;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.MySql;
using VirtoCommerce.NotificationsModule.Data.PostgreSql;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Senders;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Data.SqlServer;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.NotificationsModule.SendGrid;
using VirtoCommerce.NotificationsModule.Smtp;
using VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem;
using VirtoCommerce.NotificationsModule.Twilio;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.JsonConverters;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Security.Events;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.NotificationsModule.Web
{
    public class Module : IModule, IExportSupport, IImportSupport, IHasConfiguration
    {
        private IApplicationBuilder _appBuilder;

        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<NotificationDbContext>(options =>
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

                switch (databaseProvider)
                {
                    case "MySql":
                        options.UseMySqlDatabase(connectionString);
                        break;
                    case "PostgreSql":
                        options.UsePostgreSqlDatabase(connectionString);
                        break;
                    default:
                        options.UseSqlServerDatabase(connectionString);
                        break;
                }
            });

            serviceCollection.AddTransient<INotificationRepository, NotificationRepository>();
            serviceCollection.AddTransient<Func<INotificationRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<INotificationRepository>());
            serviceCollection.AddTransient<INotificationService, NotificationService>();
            serviceCollection.AddTransient<INotificationSearchService, NotificationSearchService>();
            serviceCollection.AddSingleton<INotificationRegistrar, NotificationRegistrar>();
            serviceCollection.AddSingleton<INotificationLayoutRegistrar, NotificationLayoutRegistrar>();
            serviceCollection.AddTransient<INotificationMessageService, NotificationMessageService>();
            serviceCollection.AddTransient<INotificationMessageSearchService, NotificationMessageSearchService>();
            serviceCollection.AddTransient<INotificationSender, NotificationSender>();
            serviceCollection.AddTransient<NotificationsExportImport>();
            serviceCollection.AddTransient<NotificationScriptObject>();

            serviceCollection.AddTransient<RequestPasswordResetHandler>();

            serviceCollection.AddTransient<INotificationLayoutService, NotificationLayoutService>();
            serviceCollection.AddTransient<INotificationLayoutSearchService, NotificationLayoutSearchService>();
            serviceCollection.AddSingleton<INotificationLayoutRegistrar, NotificationLayoutRegistrar>();

            serviceCollection.AddFileSystemTemplateLoader(opt => Configuration.GetSection("Notifications:Templates").Bind(opt));

            serviceCollection.AddSingleton<INotificationMessageSenderFactory, NotificationMessageSenderFactory>();

            serviceCollection.AddOptions<EmailSendingOptions>().Bind(Configuration.GetSection("Notifications")).ValidateDataAnnotations();
            var emailGateway = Configuration.GetValue<string>("Notifications:Gateway");
            switch (emailGateway)
            {
                case SmtpEmailNotificationMessageSender.Name:
                    {
                        serviceCollection.AddOptions<SmtpSenderOptions>().Bind(Configuration.GetSection($"Notifications:{SmtpEmailNotificationMessageSender.Name}")).ValidateDataAnnotations();
                        serviceCollection.AddTransient<INotificationMessageSender, SmtpEmailNotificationMessageSender>();
                        break;
                    }
                case SendGridEmailNotificationMessageSender.Name:
                    {
                        serviceCollection.AddOptions<SendGridSenderOptions>().Bind(Configuration.GetSection($"Notifications:{SendGridEmailNotificationMessageSender.Name}")).ValidateDataAnnotations();
                        serviceCollection.AddTransient<INotificationMessageSender, SendGridEmailNotificationMessageSender>();
                        break;
                    }
            }

            serviceCollection.AddOptions<SmsSendingOptions>().Bind(Configuration.GetSection("Notifications")).ValidateDataAnnotations();
            var smsGateway = Configuration.GetValue<string>("Notifications:SmsGateway");
            switch (smsGateway)
            {
                case TwilioSmsNotificationMessageSender.Name:
                    {
                        serviceCollection.AddOptions<TwilioSenderOptions>().Bind(Configuration.GetSection($"Notifications:{TwilioSmsNotificationMessageSender.Name}")).ValidateDataAnnotations();
                        serviceCollection.AddTransient<INotificationMessageSender, TwilioSmsNotificationMessageSender>();
                        break;
                    }
            }

            serviceCollection.AddLiquidRenderer(builder =>
            {
                builder.AddCustomLiquidFilterType(typeof(TranslationFilter));
                builder.AddCustomLiquidFilterType(typeof(UrlFilters));
                builder.AddCustomLiquidFilterType(typeof(ArrayFilter));
                builder.SetRendererLoopLimit(Configuration["Notifications:LiquidRenderOptions:LoopLimit"].TryParse(ModuleConstants.DefaultLiquidRendererLoopLimit));
                builder.SetTemplateLanguage(Configuration["Notifications:LiquidRenderOptions:TemplateLanguage"]);
            });
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            _appBuilder = appBuilder;

            AbstractTypeFactory<NotificationTemplate>.RegisterType<EmailNotificationTemplate>();
            AbstractTypeFactory<NotificationTemplate>.RegisterType<SmsNotificationTemplate>();

            AbstractTypeFactory<NotificationMessage>.RegisterType<EmailNotificationMessage>();
            AbstractTypeFactory<NotificationMessage>.RegisterType<SmsNotificationMessage>();

            AbstractTypeFactory<NotificationEntity>.RegisterType<EmailNotificationEntity>();
            AbstractTypeFactory<NotificationEntity>.RegisterType<SmsNotificationEntity>();

            AbstractTypeFactory<NotificationTemplateEntity>.RegisterType<EmailNotificationTemplateEntity>();
            AbstractTypeFactory<NotificationTemplateEntity>.RegisterType<SmsNotificationTemplateEntity>();

            AbstractTypeFactory<NotificationMessageEntity>.RegisterType<EmailNotificationMessageEntity>();
            AbstractTypeFactory<NotificationMessageEntity>.RegisterType<SmsNotificationMessageEntity>();

            AbstractTypeFactory<NotificationScriptObject>.RegisterType<NotificationScriptObject>()
                                                         .WithFactory(() => appBuilder.ApplicationServices.GetRequiredService<NotificationScriptObject>());

            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Notifications", ModuleConstants.Security.Permissions.AllPermissions);

            PolymorphJsonConverter.RegisterTypeForDiscriminator(typeof(Notification), nameof(Notification.Type));
            PolymorphJsonConverter.RegisterTypeForDiscriminator(typeof(NotificationTemplate), nameof(NotificationTemplate.Type));
            PolymorphJsonConverter.RegisterTypeForDiscriminator(typeof(NotificationMessage), nameof(NotificationMessage.Type));

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                using var notificationDbContext = serviceScope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                notificationDbContext.Database.Migrate();
            }

            appBuilder.RegisterEventHandler<UserRequestPasswordResetEvent, RequestPasswordResetHandler>();

            var defaultTemplatesDirectory = Path.Combine(ModuleInfo.FullPhysicalPath, "NotificationTemplates");
            var registrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
            registrar.RegisterNotification<ResetPasswordEmailNotification>().WithTemplatesFromPath(defaultTemplatesDirectory);
            registrar.RegisterNotification<ConfirmationEmailNotification>();
            registrar.RegisterNotification<RegistrationEmailNotification>();
            registrar.RegisterNotification<RegistrationInvitationEmailNotification>();
            registrar.RegisterNotification<RegistrationInvitationCustomerEmailNotification>();
            registrar.RegisterNotification<RemindUserNameEmailNotification>();
            registrar.RegisterNotification<ResetPasswordSmsNotification>();
            registrar.RegisterNotification<TwoFactorEmailNotification>();
            registrar.RegisterNotification<TwoFactorSmsNotification>();
            registrar.RegisterNotification<ChangePhoneNumberSmsNotification>();

            var hostLifeTime = appBuilder.ApplicationServices.GetService<IHostApplicationLifetime>();

            //Save all registered notification layouts in the database after application start
            hostLifeTime.ApplicationStarted.Register(() =>
            {
                var notificationLayoutRegistrar = appBuilder.ApplicationServices.GetService<INotificationLayoutRegistrar>();
                notificationLayoutRegistrar.SaveChanges();
            });

            //Save all registered notifications in the database after application start
            hostLifeTime.ApplicationStarted.Register(() =>
            {
                var notificationService = appBuilder.ApplicationServices.GetService<INotificationService>();
                var allRegisteredNotifications = registrar.AllRegisteredNotifications.Select(x =>
                {
                    //Do not save predefined templates in the database to prevent rewrite of exists data
                    x.ReduceDetails(NotificationResponseGroup.Default.ToString());
                    return x;
                }).ToArray();
                notificationService.SaveChangesAsync(allRegisteredNotifications).GetAwaiter().GetResult();
            });
        }

        public void Uninstall()
        {
            //Nothing to do
        }

        public async Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
        {
            await _appBuilder.ApplicationServices.GetRequiredService<NotificationsExportImport>().DoExportAsync(outStream, progressCallback, cancellationToken);
        }

        public async Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
        {
            await _appBuilder.ApplicationServices.GetRequiredService<NotificationsExportImport>().DoImportAsync(inputStream, progressCallback, cancellationToken);
        }
    }
}
