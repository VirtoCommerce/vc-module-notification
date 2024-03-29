angular.module('virtoCommerce.notificationsModule')
    .factory('virtoCommerce.notificationsModule.notificationsModuleApi', ['$resource', function ($resource) {
        return $resource('api/notifications/:id', { id: '@Id' }, {
            getNotificationList: { method: 'POST', url: 'api/notifications' },
            getNotificationByType: { method: 'GET', url: 'api/notifications/:type', },
            updateNotification: { method: 'PUT', url: 'api/notifications/:type' },
            getTemplates: { method: 'GET', url: 'api/notifications/:type/templates', isArray: true },
            getTemplateById: { method: 'GET', url: 'api/notifications/:type/templates/:id' },
            getTemplate: { method: 'GET', url: 'api/notifications/:type/:language/templates' },
            createTemplate: { method: 'POST', url: 'api/notifications/:type/templates' },
            updateTemplate: { method: 'PUT', url: 'api/notifications/:type/templates/:id' },
            renderTemplate: { method: 'POST', url: 'api/notifications/:type/templates/:language/rendercontent' },
            sharePreview: { method: 'POST', url: 'api/notifications/:type/templates/:language/sharepreview' },
            getNotificationJournalList: { method: 'POST', url: 'api/notifications/journal' },
            getNotificationJournalDetails: { method: 'GET', url: 'api/notifications/journal/:id' },
            resendNotifications: { method: 'POST', url: 'api/notifications/scheduleresend' }
        })
    }])
    .factory('virtoCommerce.notificationsModule.notificationLayoutsApi', ['$resource', function ($resource) {
        return $resource('api/notification-layouts/', {}, {
            getNotificationLayout: { method: 'GET', url: 'api/notification-layouts/:id' },
            searchNotificationLayouts: { method: 'POST', url: 'api/notification-layouts/search' },
            createNotificationLayout: { method: 'POST' },
            updateNotificationLayout: { method: 'PUT' },
            deleteNotificationLayout: { method: 'DELETE' }
        })
    }]);
