angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationJournalDetailsController',
        ['$scope', 'virtoCommerce.notificationsModule.notificationsModuleApi', 'platformWebApp.bladeNavigationService',
            function ($scope, notifications, bladeNavigationService) {
                var blade = $scope.blade;
                blade.readPermission = 'notifications:read';

                blade.initialize = function () {
                    notifications.getNotificationJournalDetails({ id: blade.currentNotificationId }, function (data) {
                        blade.currentEntity = data;
                        blade.isLoading = false;
                    }, function (error) {
                        bladeNavigationService.setError('Error ' + error.status, $scope.blade);
                    });
                }

                blade.openEmailHtmlBlade = function () {
                    var newBlade = {
                        id: 'emailHtmlContentBlade',
                        controller: 'virtoCommerce.notificationsModule.notificationJournalDetailsContentController',
                        template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-journal-details-content.html',
                        currentEntity: blade.currentEntity,
                    };

                    bladeNavigationService.showBlade(newBlade, blade);
                }

                if (blade.currentEntity.kind === 'EmailNotification') {
                    blade.toolbarCommands = [
                        {
                            name: 'platform.commands.preview',
                            icon: 'fas fa-eye',
                            executeMethod: blade.openEmailHtmlBlade,
                            canExecuteMethod: function () { return true; },
                            permission: blade.readPermission,
                        }
                    ];
                }

                blade.initialize();
            }
        ]
    );
