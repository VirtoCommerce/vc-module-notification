# Virto Commerce Notification Module

[![CI status](https://github.com/VirtoCommerce/vc-module-notification/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-notification/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification)

The Notification module provides a comprehensive infrastructure for managing and delivering notifications within the Virto Commerce platform. It encompasses the following key components:

* **Notification list**: This feature allows administrators and users to view and manage a list of notifications. The notification list provides an overview of all active and past notifications, allowing for easy tracking and monitoring.
* **Notification templates and layouts**: The module includes a flexible system for creating and managing notification templates and layouts. It leverages the power of Scriban (with Liquid support enabled) to enable dynamic content generation, making it easy to customize and personalize notifications.
* **Notification activity feed**: The activity feed feature provides a centralized hub where users can access and review all notification-related activities. It offers a comprehensive view of the notification history, including delivery status and recipient interactions.

![Notifications module](docs/media/screen-notifications-module.png)

The Notification module empowers businesses to effectively manage and deliver notifications, enabling personalized and timely communication with customers and stakeholders. With its versatile features and robust infrastructure, it ensures a seamless and engaging user experience.

## Key features

### Core Features
* **Email templates with Scriban/Liquid**: The module supports email templates using the powerful Scriban templating language, with Liquid support enabled. This allows for dynamic content generation and customization of email notifications, ensuring personalized and engaging communication with recipients.
* **Extendable model**: The notifications module offers an extendable model, allowing developers to add custom fields and attributes to notifications as per their specific business requirements. This flexibility enables seamless integration with existing systems and workflows.
* **Notification layouts**: Create reusable layout templates with common elements (header, footer, etc.) to maintain consistent branding across all notification templates.
* **Multi-language support**: Templates can be created for multiple languages to support international customers.
* **Template preview and testing**: Preview rendered templates with sample data and send test emails before going live.
* **CC and BCC support**: Send copies and blind copies of notifications to additional recipients.
* **Email attachments**: Support for adding file attachments to email notifications (API only). 

> **Important Note:** While the module supports email attachments through the API, we strongly recommend **not using attachments** for the following reasons:
> - **Size limitations**: Most SMTP servers impose strict email size limits (typically 10-25 MB), which can cause delivery failures
> - **Security concerns**: Email attachments are frequently blocked by corporate firewalls and email security filters
> - **Deliverability issues**: Emails with attachments have higher spam scores and lower delivery rates
> - **Better alternative**: Instead of attachments, include secure download links to files in your email body. This approach offers better security, no size restrictions, and improved deliverability

### Email Delivery Options
* **SMTP support**: Send emails via any SMTP server with SSL/TLS support and custom headers.
* **SendGrid support**: Integrate with SendGrid for reliable cloud-based email delivery.
* **Microsoft Graph support**: Send emails through Microsoft 365 using Microsoft Graph API.

### SMS Delivery Options
* **Twilio support**: Send SMS notifications via Twilio gateway.

### Advanced Features
* **Async delivery with retry policy**: The module implements asynchronous delivery with a built-in retry policy using Hangfire and Polly. Failed notifications are automatically retried with exponential backoff to ensure reliable delivery.
* **Notification activity logs**: All notification activities are logged, including delivery status, send attempts, and error messages.
* **File system template loader**: Load notification templates from the file system for easier version control and deployment.
* **Export/Import support**: Export and import notification configurations for backup and migration purposes.

### Database Support
* **SQL Server**: Primary database provider (default)
* **PostgreSQL**: Full support for PostgreSQL databases
* **MySQL**: Full support for MySQL databases

## Setup and Configuration

This section provides step-by-step guidance for configuring email and SMS notification delivery in your Virto Commerce application.

### Prerequisites

1. Virto Commerce Platform
2. Notification module installed
3. Access to `appsettings.json` or environment configuration

### Email Configuration

The Notification module supports three email delivery gateways: SMTP, SendGrid, and Microsoft Graph. Choose the one that best fits your requirements.

#### Option 1: SMTP Configuration

SMTP is suitable for using your own email server or third-party SMTP services like Gmail, Outlook, or custom mail servers.

Add the following configuration to your `appsettings.json`:

```json
{
  "Notifications": {
    "Gateway": "Smtp",
    "DefaultSender": "noreply@yourdomain.com",
    "DefaultReplyTo": "support@yourdomain.com",
    "Smtp": {
      "SmtpServer": "smtp.yourdomain.com",
      "Port": 587,
      "Login": "your-smtp-username",
      "Password": "your-smtp-password",
      "ForceSslTls": false,
      "CustomHeaders": {
        "X-Custom-Header": "value"
      }
    }
  }
}
```

**SMTP Configuration Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `SmtpServer` | Yes | SMTP server hostname (e.g., smtp.gmail.com) |
| `Port` | Yes | SMTP server port (common: 25, 587, 465) |
| `Login` | Yes | SMTP authentication username |
| `Password` | Yes | SMTP authentication password |
| `ForceSslTls` | No | Force SSL/TLS connection (false by default). Enable if server doesn't support STARTTLS |
| `CustomHeaders` | No | Dictionary of custom email headers |

**Example configurations for popular email providers:**

**Gmail:**
```json
{
  "Notifications": {
    "Gateway": "Smtp",
    "DefaultSender": "your-email@gmail.com",
    "Smtp": {
      "SmtpServer": "smtp.gmail.com",
      "Port": 587,
      "Login": "your-email@gmail.com",
      "Password": "your-app-specific-password"
    }
  }
}
```

> **Note:** For Gmail, you must use an [App Password](https://support.google.com/accounts/answer/185833) instead of your regular password. Enable 2-step verification first.

**Microsoft 365/Outlook:**
```json
{
  "Notifications": {
    "Gateway": "Smtp",
    "DefaultSender": "your-email@outlook.com",
    "Smtp": {
      "SmtpServer": "smtp-mail.outlook.com",
      "Port": 587,
      "Login": "your-email@outlook.com",
      "Password": "your-password"
    }
  }
}
```

**AWS SES:**
```json
{
  "Notifications": {
    "Gateway": "Smtp",
    "DefaultSender": "noreply@yourdomain.com",
    "Smtp": {
      "SmtpServer": "email-smtp.us-east-1.amazonaws.com",
      "Port": 587,
      "Login": "your-smtp-username",
      "Password": "your-smtp-password"
    }
  }
}
```

#### Option 2: SendGrid Configuration

SendGrid is a cloud-based email delivery platform that offers high deliverability and scalability.

1. **Get your SendGrid API Key:**
   - Sign up at [SendGrid](https://sendgrid.com/)
   - Navigate to Settings → API Keys
   - Create a new API key with "Mail Send" permissions
   - Copy the generated API key

2. **Add the configuration to your `appsettings.json`:**

```json
{
  "Notifications": {
    "Gateway": "SendGrid",
    "DefaultSender": "noreply@yourdomain.com",
    "DefaultReplyTo": "support@yourdomain.com",
    "SendGrid": {
      "ApiKey": "SG.your-api-key-here"
    }
  }
}
```

**SendGrid Configuration Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `ApiKey` | Yes | SendGrid API key with Mail Send permissions |

> **Important:** Never commit API keys to source control. Use environment variables or secure configuration providers in production.

#### Option 3: Microsoft Graph Configuration

Microsoft Graph allows sending emails through Microsoft 365 accounts using Azure AD application authentication.

1. **Register an Azure AD Application:**
   - Go to [Azure Portal](https://portal.azure.com/)
   - Navigate to Azure Active Directory → App registrations
   - Click "New registration"
   - Provide a name and register the application
   - Note the **Application (client) ID** and **Directory (tenant) ID**

2. **Create a Client Secret:**
   - In your app registration, go to Certificates & secrets
   - Create a new client secret
   - Copy the secret value immediately (it won't be shown again)

3. **Grant API Permissions:**
   - In your app registration, go to API permissions
   - Add permission → Microsoft Graph → Application permissions
   - Add: `Mail.Send`
   - Click "Grant admin consent"

4. **Add the configuration to your `appsettings.json`:**

```json
{
  "Notifications": {
    "Gateway": "MicrosoftGraph",
    "DefaultSender": "noreply@yourdomain.com",
    "MicrosoftGraph": {
      "ApplicationId": "your-application-id",
      "TenantId": "your-tenant-id",
      "SecretValue": "your-client-secret"
    }
  }
}
```

**Microsoft Graph Configuration Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `ApplicationId` | Yes | Azure AD application (client) ID |
| `TenantId` | Yes | Azure AD directory (tenant) ID |
| `SecretValue` | Yes | Client secret value |

### SMS Configuration

The Notification module supports SMS delivery via Twilio.

#### Twilio SMS Configuration

1. **Get your Twilio credentials:**
   - Sign up at [Twilio](https://www.twilio.com/)
   - Get a phone number capable of sending SMS
   - From your Twilio Console, copy your Account SID and Auth Token

2. **Add the configuration to your `appsettings.json`:**

```json
{
  "Notifications": {
    "SmsGateway": "Twilio",
    "SmsDefaultSender": "+1234567890",
    "Twilio": {
      "AccountId": "your-account-sid",
      "AccountPassword": "your-auth-token"
    }
  }
}
```

**Twilio Configuration Parameters:**

| Parameter | Required | Description |
|-----------|----------|-------------|
| `AccountId` | Yes | Twilio Account SID |
| `AccountPassword` | Yes | Twilio Auth Token |
| `SmsDefaultSender` | Yes | Default sender phone number in E.164 format |

### Advanced Configuration

#### Template Loading from File System

Load notification templates from the file system for easier version control:

```json
{
  "Notifications": {
    "Templates": {
      "DiscoveryPath": "App_Data/NotificationTemplates",
      "FallbackDiscoveryPath": "App_Data/NotificationTemplatesDefault"
    }
  }
}
```

Templates should follow this naming convention:
- Subject: `NotificationName_subject.lang.txt`
- Body: `NotificationName_body.lang.html`
- Sample: `NotificationName_sample.lang.json`

Example: `OrderConfirmation_body.en-US.html`

#### Liquid Renderer Options

Configure the Liquid template renderer:

```json
{
  "Notifications": {
    "LiquidRenderOptions": {
      "LoopLimit": 1000,
      "TemplateLanguage": "Liquid"
    }
  }
}
```

#### Database Provider Configuration

Choose your database provider in `appsettings.json`:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "VirtoCommerce": "Data Source=(local);Initial Catalog=VirtoCommerce3;..."
  }
}
```

Supported values for `DatabaseProvider`:
- `SqlServer` (default)
- `PostgreSql`
- `MySql`

### Environment Variables Configuration

For production environments, use environment variables instead of hardcoding sensitive data:

**Linux/macOS:**
```bash
export Notifications__Gateway="SendGrid"
export Notifications__SendGrid__ApiKey="your-api-key"
export Notifications__DefaultSender="noreply@yourdomain.com"
```

**Windows PowerShell:**
```powershell
$env:Notifications__Gateway="SendGrid"
$env:Notifications__SendGrid__ApiKey="your-api-key"
$env:Notifications__DefaultSender="noreply@yourdomain.com"
```

**Docker:**
```yaml
environment:
  - Notifications__Gateway=SendGrid
  - Notifications__SendGrid__ApiKey=your-api-key
  - Notifications__DefaultSender=noreply@yourdomain.com
```

### Verifying Configuration

After configuration, verify your setup:

1. Start your Virto Commerce application
2. Navigate to the Notifications module in the admin panel
3. Create or select a notification template
4. Use the "Preview" button to render the template
5. Use the "Share" button to send a test email to verify delivery

Check the notification logs in the "Send log" section for delivery status and error messages if issues occur.

### Troubleshooting

**Email not sending:**
- Verify your gateway configuration parameters
- Check firewall rules for SMTP ports (587, 465, 25)
- Review notification logs for specific error messages
- Ensure DefaultSender email is valid and authorized
- For SMTP: Test credentials with a mail client first
- For SendGrid: Verify API key has Mail Send permissions
- For Microsoft Graph: Confirm API permissions are granted

**Templates not rendering:**
- Check template syntax for Liquid/Scriban errors
- Verify sample data JSON is valid
- Review application logs for rendering errors

**SMS not sending:**
- Verify Twilio credentials
- Ensure sender phone number is verified in Twilio
- Check recipient phone number format (must be E.164)
- Review Twilio console for delivery status


## Documentation

* [Notification module user documentation](https://docs.virtocommerce.org/platform/user-guide/notifications/overview/)
* [Notification module developer guide](https://docs.virtocommerce.org/platform/developer-guide/Fundamentals/Notifications/overview/)
* [REST API](https://virtostart-demo-admin.govirto.com/docs/index.html?urls.primaryName=VirtoCommerce.Notifications)  
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-notification/)
* [Liquid as Primary Template Language](https://community.virtocommerce.com/t/liquid-as-primary-template-language/78)
* [Creating Email templates](https://docs.virtocommerce.org/platform/user-guide/notifications/notification-templates/)

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-notification/releases/latest)

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
provided under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
