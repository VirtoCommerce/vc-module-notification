using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VirtoCommerce.NotificationsModule.Twilio;
using VirtoCommerce.Notifications.Core.Types;
using VirtoCommerce.NotificationsModule.Core;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.NotificationsModule.Data.ExportImport;
using VirtoCommerce.NotificationsModule.Data.Model;
using VirtoCommerce.NotificationsModule.Data.Repositories;
using VirtoCommerce.NotificationsModule.Data.Senders;
using VirtoCommerce.NotificationsModule.Data.Services;
using VirtoCommerce.NotificationsModule.Data.TemplateLoaders;
using VirtoCommerce.NotificationsModule.LiquidRenderer;
using VirtoCommerce.NotificationsModule.LiquidRenderer.Filters;
using VirtoCommerce.NotificationsModule.SendGrid;
using VirtoCommerce.NotificationsModule.Smtp;
using VirtoCommerce.NotificationsModule.Web.JsonConverters;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Notifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.NotificationsModule.Web
{
    public class Module : IModule, IExportSupport, IImportSupport
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        private IApplicationBuilder _appBuilder;

        public void Initialize(IServiceCollection serviceCollection)
        {
            var snapshot = serviceCollection.BuildServiceProvider();
            var configuration = snapshot.GetService<IConfiguration>();
            serviceCollection.AddDbContext<NotificationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("VirtoCommerce"),
                opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds)));
            serviceCollection.AddTransient<INotificationRepository, NotificationRepository>();
            serviceCollection.AddTransient<Func<INotificationRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<INotificationRepository>());
            serviceCollection.AddTransient<INotificationService, NotificationService>();
            serviceCollection.AddTransient<INotificationSearchService, NotificationSearchService>();
            serviceCollection.AddSingleton<INotificationRegistrar, NotificationRegistrar>();
            serviceCollection.AddTransient<INotificationMessageService, NotificationMessageService>();
            serviceCollection.AddTransient<INotificationMessageSearchService, NotificationMessageSearchService>();
            serviceCollection.AddTransient<INotificationSender, NotificationSender>();
            serviceCollection.AddTransient<IEmailSender, EmailNotificationMessageSender>();
            serviceCollection.AddTransient<NotificationsExportImport>();
            serviceCollection.AddTransient<NotificationScriptObject>();

            serviceCollection.AddTransient<INotificationTemplateLoader, FileSystemNotificationTemplateLoader>();
            serviceCollection.AddOptions<FileSystemTemplateLoaderOptions>().Bind(configuration.GetSection("Notifications:Templates")).ValidateDataAnnotations();

            serviceCollection.AddSingleton<INotificationMessageSenderProviderFactory, NotificationMessageSenderProviderFactory>();

            serviceCollection.AddOptions<EmailSendingOptions>().Bind(configuration.GetSection("Notifications")).ValidateDataAnnotations();
            var emailGateway = configuration.GetValue<string>("Notifications:Gateway");
            if (emailGateway.Equals("Smtp"))
            {
                serviceCollection.AddOptions<SmtpSenderOptions>().Bind(configuration.GetSection("Notifications:Smtp")).ValidateDataAnnotations();
                serviceCollection.AddTransient<INotificationMessageSender, SmtpEmailNotificationMessageSender>();
            }
            else if (emailGateway.Equals("SendGrid"))
            {
                serviceCollection.AddOptions<SendGridSenderOptions>().Bind(configuration.GetSection("Notifications:SendGrid")).ValidateDataAnnotations();
                serviceCollection.AddTransient<INotificationMessageSender, SendGridEmailNotificationMessageSender>();
            }

            serviceCollection.AddOptions<SmsSendingOptions>().Bind(configuration.GetSection("Notifications")).ValidateDataAnnotations();
            var smsGateway = configuration.GetValue<string>("Notifications:SmsGateway");
            switch (smsGateway)
            {
                case "Twilio":
                    {
                        serviceCollection.AddOptions<TwilioSenderOptions>().Bind(configuration.GetSection("Notifications:Twilio")).ValidateDataAnnotations();
                        serviceCollection.AddTransient<INotificationMessageSender, TwilioSmsNotificationMessageSender>();
                        break;
                    }
            }

            serviceCollection.AddLiquidRenderer(builder =>
            {
                builder.AddCustomLiquidFilterType(typeof(TranslationFilter));
                builder.AddCustomLiquidFilterType(typeof(UrlFilters));
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
            permissionsRegistrar.RegisterPermissions(ModuleConstants.Security.Permissions.AllPermissions.Select(x =>
                new Permission()
                {
                    GroupName = "Notifications",
                    ModuleId = ModuleInfo.Id,
                    Name = x
                }).ToArray());

            var mvcJsonOptions = appBuilder.ApplicationServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>();
            mvcJsonOptions.Value.SerializerSettings.Converters.Add(new NotificationPolymorphicJsonConverter());

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                using (var notificationDbContext = serviceScope.ServiceProvider.GetRequiredService<NotificationDbContext>())
                {
                    notificationDbContext.Database.EnsureCreated();
                    notificationDbContext.Database.Migrate();
                }
            }

            //TODO move out from here to projects
            var configuration = appBuilder.ApplicationServices.GetService<IConfiguration>();
            var emailGateway = configuration.GetValue<string>("Notifications:Gateway");
            var notificationMessageSenderProviderFactory = appBuilder.ApplicationServices.GetService<INotificationMessageSenderProviderFactory>();
            switch (emailGateway)
            {
                case "SendGrid":
                    notificationMessageSenderProviderFactory.RegisterSenderForType<EmailNotification, SendGridEmailNotificationMessageSender>();
                    break;
                default:
                    notificationMessageSenderProviderFactory.RegisterSenderForType<EmailNotification, SmtpEmailNotificationMessageSender>();
                    break;
            }

            var smsGateway = configuration.GetValue<string>("Notifications:SmsGateway");
            switch (smsGateway)
            {
                case "Twilio":
                    notificationMessageSenderProviderFactory.RegisterSenderForType<SmsNotification, TwilioSmsNotificationMessageSender>();
                    break;
            }

            var registrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
            registrar.RegisterNotification<ResetPasswordEmailNotification>();
            registrar.RegisterNotification<ConfirmationEmailNotification>();
            registrar.RegisterNotification<RegistrationEmailNotification>();
            registrar.RegisterNotification<RegistrationInvitationEmailNotification>();
            registrar.RegisterNotification<RemindUserNameEmailNotification>();
            registrar.RegisterNotification<ResetPasswordSmsNotification>();
            registrar.RegisterNotification<TwoFactorEmailNotification>();
            registrar.RegisterNotification<TwoFactorSmsNotification>();
            registrar.RegisterNotification<ChangePhoneNumberSmsNotification>();
        }

        public void Uninstall()
        {
            //Nothing todo
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
