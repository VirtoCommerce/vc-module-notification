# Developing Your Custom Notification

## Overview

This guide describes how to define notificationss and notification predefinitions, and how to send them.

## Registering Your Own Notification

To register a new notification, do the following:

1. Create a notification and give it a name (e.g., `SampleEmailNotification`), basing it on the `EmailNotification` class (there is also another standard class, `SmsNotification`):

    ```cs
    public class SampleEmailNotification : EmailNotification
    {
        public SampleEmailNotification() : base(nameof(SampleEmailNotification)) {}
    }
    ```
	
> <!---change to tip in new layout-->You can find some code samples [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Types/SampleEmailNotification.cs).

2. Get the `INotificationRegistrar` service in the `PostInitialize` method of the `Module.cs` file:

    ```cs
    var registrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
    ```
	
3. Call the `RegisterNotification` method and set the generic type parameter to `SampleEmailNotification`:

    ```cs
    registrar.RegisterNotification<SampleEmailNotification>();
    ```
	
    > <!---change to tip in new layout-->You can find some code samples [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L59)

## Setting Default Notification Templates and Sample Data
There are two ways to declare and distribute predefined notification templates and sample data:

1. Using the files included into the module bundle
2. Through inline definition in the source code

### Using Module Bundle
This is the main way to distribute defaults. Commerce bundled VC-modules store notification predifinitions this way. To do the same for your custom module, do the following:

1. For every notification type, create a subject template of the email, a body template, and sample data in three different files, according to the `[NotificationName]_[PartPostfixe]` file name convention, where `PartPostfixe` means:

|PartPostfixe|Description|
|-|-|
|subject.txt|UTF-8 text file with the subject rendering template|
|body.html|Body rendering template|
|sample.json|Sample object JSON file|

2. Put the files to a folder inside the `*.Web` project of your module (for example, `Templates`).
3. Modify the `*.Web.csproj` file to allow the predifinition file to be copied upon publishing. Technically, you need to add the following lines:

```
    <ItemGroup>
        <NotificationTemplates Include="Templates/**" />
    </ItemGroup>
    <Target Name="CopyCustomContentOnPublish" AfterTargets="Publish">
        <Copy SourceFiles="@(NotificationTemplates)" DestinationFiles="$(PublishDir)\..\%(Identity)" />
    </Target>
```

4. Use the `WithTemplatesFromPath` extension to attach predifinitions from the folder while registering the notification type in `Module.cs`:

```cs
var moduleTemplatesPath = Path.Combine(ModuleInfo.FullPhysicalPath, "Templates");
registrar.RegisterNotification<SampleEmailNotification>().WithTemplatesFromPath(Path.Combine(moduleTemplatesPath, "Custom"), Path.Combine(moduleTemplatesPath, "Default"));
```    

> <!---change to tip in new layout-->You can find the full sample [here](https://github.com/VirtoCommerce/vc-module-notification/tree/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Templates). Also, we encourage you to view how it works in a real commerce-bundle module [here](https://github.com/VirtoCommerce/vc-module-order/tree/dev/src/VirtoCommerce.OrdersModule.Web).

### Hardcode as Inline Definition: 
There is a less useful but still possible way to define predifinitions directly in the source code:

```cs
registrar.RegisterNotification<SampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
        {
            Subject = "Sample subject",
            Body = "<p>Sample text</p>",
        });
```

You can also do so from an assembly embedded resource:

```cs
var assembly = Assembly.GetExecutingAssembly();
registrar.RegisterNotification<SampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
        {
            Subject = assembly.GetManifestResourceStream("VirtoCommerce.NotificationsSampleModule.Web.Templates.SampleEmailNotification_subject.txt").ReadToString(),
            Body = assembly.GetManifestResourceStream("VirtoCommerce.NotificationsSampleModule.Web.Templates.SampleEmailNotification_body.html").ReadToString()
        });
```

> <!---change to tip in new layout-->You can find some code samples [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L59).

## Sending Notification from Code

1. After registering your notification, you need to call two services: `INotificationSearchService` and `INotificationSender`.
1. Assuming we have 'SampleService' as a sample, we need to add the services to the constructor this way:

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
	
1. Get the notification via `INotificationSearchService` through the method you need:

    ```cs
    var notification = await _notificationSearchService.GetNotificationAsync<SampleEmailNotification>();
    ```
	
1. Set all notification parameters for the notification, like this:

    ```cs
     notification.LanguageCode = 'en-US';
     notification.SetFromToMembers("from@test.com", "to@test.com");    
    ```
	
1. Send the notification instantly:

    ```cs
    await _notificationSender.SendNotificationAsync(notification);
    ```
	
    Or schedule it like this:

    ```cs
    _notificationSender.ScheduleSendNotification(notification);
    ``` 
	
> > <!---change to tip in new layout-->You can find our demo code samples [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Services/SampleService.cs).     

## Extending Existing Notification Type and Template

If you want to extend an existing notification, you need to:

1. Create a notification based on the derived notification:

    ```cs
    public class ExtendedSampleEmailNotification : SampleEmailNotification
    {
        public ExtendedSampleEmailNotification() : base(nameof(ExtendedSampleEmailNotification))
        {
        }
    }
    ```
	
> <!---change to tip in new layout-->We have some sample code [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Types/ExtendedSampleEmailNotification.cs).

2. Override the notification type through `INotificationRegistrar`:

    ```cs
    registrar.OverrideNotificationType<SampleEmailNotification, ExtendedSampleEmailNotification>();
    ```
	
> <!---change to tip in new layout-->Feel free to use sample code [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L66).

3. Add templates for the extended notification, if required:

    ```cs
    registrar.OverrideNotificationType<SampleEmailNotification, ExtendedSampleEmailNotification>().WithTemplates(new EmailNotificationTemplate()
            {
                Subject = "Extended SampleEmailNotification subject",
                Body = "Extended SampleEmailNotification body test"
            });
    ```
	
> <!---change to tip in new layout-->You can find some sample code [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Module.cs#L66).

4. Define the derived notifications with their own types and convert the types to the based type, e.g., `SampleEmailNotification`. This can be defined through migration: 
    + Create a clean migration in your project.
    + Add an SQL-script that will be updating your notifications.
 
> <!---change to tip in new layout--> You can find a notification example [here](https://github.com/VirtoCommerce/vc-module-notification/blob/dev/samples/VirtoCommerce.NotificationsSampleModule.Web/Migrations/20200407123225_OverridingNotificationsForBackwardV2.cs)   

> <!---change to note in new layout--> You can find all our notification samples [here](https://github.com/VirtoCommerce/vc-module-notification/tree/dev/samples/VirtoCommerce.NotificationsSampleModule.Web).
