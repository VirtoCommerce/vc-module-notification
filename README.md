# Overview

[![CI status](https://github.com/VirtoCommerce/vc-module-notification/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-notification/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-notification&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-notification)

The Notifications module provides a comprehensive infrastructure for managing and delivering notifications within the Virto Commerce platform. It encompasses the following key components:

1. Notification List: This feature allows administrators and users to view and manage a list of notifications. The notification list provides an overview of all active and past notifications, allowing for easy tracking and monitoring.
2. Notification Templates and Layouts: The module includes a flexible system for creating and managing notification templates and layouts. It leverages the power of Scriban (with Liquid support enabled) to enable dynamic content generation, making it easy to customize and personalize notifications.
3. Notification Activity Feed: The activity feed feature provides a centralized hub where users can access and review all notification-related activities. It offers a comprehensive view of the notification history, including delivery status and recipient interactions.

![Notifications module](docs/media/screen-notifications-module.png)

The Notifications module empowers businesses to effectively manage and deliver notifications, enabling personalized and timely communication with customers and stakeholders. With its versatile features and robust infrastructure, it ensures a seamless and engaging user experience.

## Key Features

1. Email Templates with Scriban
1. Extendable Model
1. SMTP and SendGrid Suppor
1. Async Delivery with Retry Policy

### Email Templates with Scriban
The module supports email templates using the powerful Scriban templating language, with Liquid support enabled. This allows for dynamic content generation and customization of email notifications, ensuring personalized and engaging communication with recipients.

### Extendable Model
The notifications module offers an extendable model, allowing developers to add custom fields and attributes to notifications as per their specific business requirements. This flexibility enables seamless integration with existing systems and workflows.

### SMTP and SendGrid Support
The module provides support for multiple email delivery options, including SMTP and SendGrid. This ensures reliable and efficient email delivery, enabling seamless communication with customers and stakeholders.

### Async Delivery with Retry Policy
To enhance performance and reliability, the notifications module implements asynchronous delivery with a built-in retry policy. This ensures that notifications are delivered promptly and efficiently, even in case of temporary failures or network interruptions.

## Documentation

1. [Notification Module Document](/docs/index.md)
1. [Developer Instructions](/docs/tech-doc.md)
1. [View on GitHub](https://github.com/VirtoCommerce/vc-module-notification/tree/dev)
1. [Customer_order Object in Notification Templates](https://community.virtocommerce.com/t/whats-customer-order-object-in-the-notifications-templates/97)
1. [Liquid as Primary Template Language](https://community.virtocommerce.com/t/liquid-as-primary-template-language/78)
1. [Tips and Tricks for Creating Email Templates](/docs/tips-and-tricks-for-creating-email-templates.md)

## References

1. Deployment: https://virtocommerce.com/docs/latest/developer-guide/deploy-module-from-source-code/
1. Installation: https://www.virtocommerce.com/docs/latest/user-guide/modules/
1. Home: https://virtocommerce.com
1. Community: https://www.virtocommerce.org
1. [Download Latest Release](https://github.com/VirtoCommerce/vc-module-notification/releases/)

## License

Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
provided under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
