# Developing your custom notification

## Overview

The documentation describes how to define notifications, notification predefinitions and how to send.

## How to register your own notification

If you would like to register a notification e.g. 'SampleEmailNotification':
1. Create the notification with name 'SampleEmailNotification' and based on 'EmailNotification' (also there is a standard based class 'SmsNotification')
    ```cs
    public class SampleEmailNotification : EmailNotification
    {
        public SampleEmailNotification() : base(nameof(SampleEmailNotification)) {}
    }
    ```
    look at [the code](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Types/SampleEmailNotification.cs)
1. Get a service `INotificationRegistrar` in `PostInitialize` method of `Module.cs`.
    ```cs
    var registrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
    ```    
1. Call a method `RegisterNotification` and set generic type parameter to `SampleEmailNotification`
    ```cs
    registrar.RegisterNotification<SampleEmailNotification>();
    ```
    >look at [the code](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L59)

## Set default notification templates and sample data
There are two ways to declare and distribute predefined notification templates and sample data:
1. In files included to the module bundle
2. Inline definition in the source code

### Use module bundle
This is the main way to distribute defaults. Commerce bundled VC-modules store notification predifinitions this manner. To do the same in your custom module, do the followings:
1. For every notification type: create subject template of the mail, body template and sample data in three different files, accordingly to the conventioned filename template: `[NotificationName]_[PartPostfixe]`, where `PartPostfixe`:

|PartPostfixe|Description|
|-|-|
|subject.txt|UTF-8 text file with subject rendering template|
|body.html|Body rendering template|
|sample.json|Sample object JSON|

2. Place files to some folder inside of `*.Web` project of your module (for example, `Templates`)
3. Modify `*.Web.csproj` file to allow predifinitions file to be copied on publishing. Add following lines:
```
    <ItemGroup>
        <NotificationTemplates Include="Templates/**" />
    </ItemGroup>
    <Target Name="CopyCustomContentOnPublish" AfterTargets="Publish">
        <Copy SourceFiles="@(NotificationTemplates)" DestinationFiles="$(PublishDir)\..\%(Identity)" />
    </Target>
```
4. Use `WithTemplatesFromPath` extension to attach predifinitions from the folder while registering notification type in `Module.cs`:
```cs
var moduleTemplatesPath = Path.Combine(ModuleInfo.FullPhysicalPath, "Templates");
registrar.RegisterNotification<SampleEmailNotification>().WithTemplatesFromPath(Path.Combine(moduleTemplatesPath, "Custom"), Path.Combine(moduleTemplatesPath, "Default"));
```    

Look full sample at [samples](https://github.com/VirtoCommerce/vc-module-notification/tree/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Templates). Also, we recommend to see how it's made in real commerce-bundle module, like [OrderModule](https://github.com/VirtoCommerce/vc-module-order/tree/dev/src/VirtoCommerce.OrdersModule.Web).

### Hardcode as inline definition: 
There are less useful, but possible way to define predifinitions directly in source code:
```cs
registrar.RegisterNotification<SampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
        {
            Subject = "Sample subject",
            Body = "<p>Sample text</p>",
        });
``` 
...or from  assembly embedded resource:
```cs
var assembly = Assembly.GetExecutingAssembly();
registrar.RegisterNotification<SampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
        {
            Subject = assembly.GetManifestResourceStream("VirtoCommerce.NotificationsSampleModule.Web.Templates.SampleEmailNotification_subject.txt").ReadToString(),
            Body = assembly.GetManifestResourceStream("VirtoCommerce.NotificationsSampleModule.Web.Templates.SampleEmailNotification_body.html").ReadToString()
        });
```
>look at [the sample code](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L59)

## How to send notification from code

1. After registration the notification need to call two services: `INotificationSearchService` and `INotificationSender`
1. There is a service 'SampleService' for example
1. Need to add the services to constructor
    ```cs
    public class SampleService 
    {
        private readonly INotificationSearchService _notificationSearchService;
        private readonly INotificationSender _notificationSender;

        public SampleService(INotificationSender notificationSender, INotificationSearchService notificationSearchService)
        {
            _notificationSender = notificationSender;
            _notificationSearchService = notificationSearchService;
        }
    }
    ```
1. Then need to get the notification via `INotificationSearchService` in needed method
    ```cs
    var notification = await _notificationSearchService.GetNotificationAsync<SampleEmailNotification>();
    ```
1. Then set all notification parameters for the notification like this
    ```cs
     notification.LanguageCode = 'en-US';
     notification.SetFromToMembers("from@test.com", "to@test.com");    
    ```
1. Then to send the notification instantly
    ```cs
    await _notificationSender.SendNotificationAsync(notification);
    ```
    or schedule
    ```cs
    _notificationSender.ScheduleSendNotification(notification);
    ``` 
> NOTE: look at [demo-code](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Services/SampleService.cs)        

## How to extend an exist notification type and template    

1. If would like to extend an exist notification then create a extend notification based on a derived notification
    ```cs
    public class ExtendedSampleEmailNotification : SampleEmailNotification
    {
        public ExtendedSampleEmailNotification() : base(nameof(ExtendedSampleEmailNotification))
        {
        }
    }
    ```
    look at [code](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Types/ExtendedSampleEmailNotification.cs)
1. Then need to override the notification type via `INotificationRegistrar`
    ```cs
    registrar.OverrideNotificationType<SampleEmailNotification, ExtendedSampleEmailNotification>();
    ```
    look at [code](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L66)
1. Also there is a possibility to add templates for the extended notification 
    ```cs
    registrar.OverrideNotificationType<SampleEmailNotification, ExtendedSampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
            {
                Subject = "Extended SampleEmailNotification subject",
                Body = "Extended SampleEmailNotification body test"
            });
    ```
    look at [code](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L66)
1. And need to define derived notifications where has own types and convert the types to based type (like as SampleEmailNotification). It can be define with the Migration. 
    1. Need to create a clean migration in project.
    1. Then to add SQL-script which will be update notifications. 
>Look at [example](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Migrations/20200407123225_OverridingNotificationsForBackwardV2.cs)   

#### NOTE: Look at all samples in [project](https://github.com/VirtoCommerce/vc-module-notification/tree/dev/samples/VirtoCommerce.NotificationsSampleModule.Web)
