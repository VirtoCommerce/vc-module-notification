//Call this to register our module to main application
var moduleTemplateName = "virtoCommerce.notificationsModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleTemplateName);
}

angular.module(moduleTemplateName, [])
    .config(['$stateProvider', '$urlRouterProvider',
        function ($stateProvider, $urlRouterProvider) {
            $stateProvider
                .state('workspace.notificationsModule', {
                    url: '/notifications?objectId&objectTypeId&type&templateId',
                    // We update the query string ourselves (type/templateId) to make templates linkable;
                    // don't reload the state (and rebuild the blade stack) when only the search changes.
                    reloadOnSearch: false,
                    templateUrl: '$(Platform)/Scripts/common/templates/home.tpl.html',
                    controller: ['$scope', '$location', '$translate', 'platformWebApp.bladeNavigationService', 'platformWebApp.settings',
                        'virtoCommerce.notificationsModule.notificationsModuleApi',
                        'virtoCommerce.notificationsModule.notificationTemplatesResolverService',
                        function ($scope, $location, $translate, bladeNavigationService, settings, notifications, notificationTemplatesResolverService) {
                            var menuBlade = {
                                id: 'notifications',
                                title: 'platform.menu.notifications',
                                subtitle: 'platform.blades.notifications-menu.subtitle',
                                controller: 'virtoCommerce.notificationsModule.notificationsMenuController',
                                template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notifications-menu.tpl.html',
                                isClosingDisabled: true
                            };
                            bladeNavigationService.showBlade(menuBlade);

                            // Deep link: /notifications?type=<notificationType>&templateId=<id> loads the
                            // notification and opens the template editor blade directly (no intermediate
                            // notification-details / template-list blades).
                            var type = $location.search().type;
                            var templateId = $location.search().templateId;
                            if (!type) { return; }

                            notifications.getNotificationByType({ type: type }, function (notification) {
                                // Without templateId fall back to the template that has no id — the
                                // predefined one that comes from module resources.
                                var template = templateId
                                    ? _.find(notification.templates, function (x) { return x.id === templateId; })
                                    : _.find(notification.templates, function (x) { return !x.id && !x.languageCode; })
                                        || _.find(notification.templates, function (x) { return !x.id; });
                                if (!template) { return; }

                                var resolved = notificationTemplatesResolverService.resolve(notification.kind);
                                if (!resolved || !resolved.detailBlade) { return; }

                                settings.getValues({ id: 'VirtoCommerce.Core.General.Languages' }, function (languages) {
                                    var editBlade = {
                                        id: resolved.detailBlade.id,
                                        title: 'notifications.blades.notifications-edit-template.title',
                                        titleValues: { displayName: $translate.instant('notificationTypes.' + notification.type + '.displayName') },
                                        notification: notification,
                                        languageCode: template.languageCode,
                                        editedTemplate: template,
                                        isNew: false,
                                        isFirst: false,
                                        isDeepLink: true,
                                        languages: languages,
                                        kind: notification.kind,
                                        controller: resolved.detailBlade.controller,
                                        template: resolved.detailBlade.template
                                    };
                                    bladeNavigationService.showBlade(editBlade, menuBlade);
                                });
                            });
                        }
                    ]
                });
        }
    ])
    // define search filters to be accessible platform-wide
    .factory('virtoCommerce.notificationsModule.predefinedSearchFilters', ['$localStorage', function ($localStorage) {
        $localStorage.notificationsJournalSearchFilters = $localStorage.notificationsJournalSearchFilters || [];

        return {
            register: function (currentFiltersUpdateTime, currentFiltersStorageKey, newFilters) {
                _.each(newFilters, function (newFilter) {
                    var found = _.find($localStorage.notificationsJournalSearchFilters, function (x) {
                        return x.id == newFilter.id;
                    });
                    if (found) {
                        if (found && (!found.lastUpdateTime || found.lastUpdateTime < currentFiltersUpdateTime)) {
                            angular.copy(newFilter, found);
                        }
                    } else if (!$localStorage[currentFiltersStorageKey] || $localStorage[currentFiltersStorageKey] < currentFiltersUpdateTime) {
                        $localStorage.notificationsJournalSearchFilters.splice(0, 0, newFilter);
                    }
                });

                $localStorage[currentFiltersStorageKey] = currentFiltersUpdateTime;
            }
        };
    }])
    .run(['platformWebApp.mainMenuService', 'platformWebApp.widgetService', '$state', 'virtoCommerce.notificationsModule.notificationTypesResolverService',
        'virtoCommerce.notificationsModule.notificationTemplatesResolverService', 'platformWebApp.dynamicTemplateService', 'virtoCommerce.notificationsModule.predefinedSearchFilters',
        function (mainMenuService, widgetService, $state, notificationTypesResolverService,
            notificationTemplatesResolverService, dynamicTemplateService, predefinedSearchFilters) {
            //Register module in main menu
            var menuItem = {
                path: 'browse/notificationsModule',
                icon: 'fa fa-envelope',
                title: 'notifications.main-menu-title',
                priority: 7,
                action: function () { $state.go('workspace.notificationsModule'); },
                permission: 'notifications:access'
            };
            mainMenuService.addMenuItem(menuItem);

            widgetService.registerWidget({
      	        controller: 'virtoCommerce.notificationsModule.notificationsTemplatesWidgetController',
      	        template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/widgets/notificationsTemplatesWidget.tpl.html'
      	    }, 'notificationsDetail');
            widgetService.registerWidget({
      	        controller: 'virtoCommerce.notificationsModule.notificationsLogWidgetController',
      	        template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/widgets/notificationsLogWidget.tpl.html'
      	    }, 'notificationsDetail');
            
            // register types
            notificationTypesResolverService.registerType({
                type: 'EmailNotification',
                icon: 'fa fa-envelope',
                detailBlade: {
                  template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-details.tpl.html',
                  controller: 'virtoCommerce.notificationsModule.notificationsEditController'
                },
                knownChildrenTypes: ['Email', 'Sms']
            }); 

            // register types
            notificationTypesResolverService.registerType({
                type: 'SmsNotification',
                icon: 'fa fa-comment',
                detailBlade: {
                  template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-sms-details.tpl.html',
                  controller: 'virtoCommerce.notificationsModule.notificationsEditController'
                },
                knownChildrenTypes: ['Email', 'Sms']
            }); 

            // register templates
            notificationTemplatesResolverService.registerTemplate({
                type: 'EmailNotification',
                icon: 'fa fa-envelope',
                detailBlade: {
                  template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notifications-edit-template.tpl.html',
                },
                knownChildrenTypes: ['Email', 'Sms']
            }); 

            // register templates
            notificationTemplatesResolverService.registerTemplate({
                type: 'SmsNotification',
                icon: 'fa fa-comment',
                detailBlade: {
                  template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notifications-edit-template.tpl.html',
                },
                knownChildrenTypes: ['Email', 'Sms']
            });
            
            // predefine search filters for search
            predefinedSearchFilters.register(1485892981, 'notificationsJournalSearchFiltersDate', [
                { name: 'notifications.blades.notifications-journal.labels.filter-new' },
                { keyword: 'isActive:true, isSuccessSend:false', id: 2, name: 'notifications.blades.notifications-journal.labels.filter-only-pending' },
                { keyword: 'isActive:false, isSuccessSend:false', id: 1, name: 'notifications.blades.notifications-journal.labels.filter-with-errors' }
                
            ]);
            
            dynamicTemplateService.ensureTemplateLoaded('Modules/$(VirtoCommerce.Notifications)/Scripts/directives/itemSearch.tpl.html');
    }]);
