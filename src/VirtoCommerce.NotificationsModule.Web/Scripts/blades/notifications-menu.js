angular.module('virtoCommerce.notificationsModule')
.controller('virtoCommerce.notificationsModule.notificationsMenuController', ['$scope', '$stateParams', 'platformWebApp.bladeNavigationService', function ($scope, $stateParams, bladeNavigationService) {
    var blade = $scope.blade;
    blade.updatePermission = 'notifications:read';

    function initializeBlade() {
        var entities = [
            {
                id: '3',
                name: 'notifications.blades.notifications-journal.title',
                subtitle: 'notifications.blades.notifications-journal.subtitle',
                icon: 'fa fa-book',
                templateName: 'notifications-journal',
                controllerName: 'notificationsJournalController'
            },
            {
                id: '1',
                name: 'notifications.blades.notifications-list.title',
                subtitle: 'notifications.blades.notifications-list.subtitle',
                icon: 'fa fa-list',
                templateName: 'notifications-list',
                controllerName: 'notificationsListController'
            },
            {
                id: '2',
                name: 'notifications.blades.notification-layouts-list.title',
                subtitle: 'notifications.blades.notification-layouts-list.subtitle',
                icon: 'fa fa-table',
                templateName: 'notification-layouts-list',
                controllerName: 'notificationLayoutsListController'
            }
            ];
        blade.currentEntities = entities;
        blade.isLoading = false;
    };

    blade.openBlade = function (data) {
        if (!blade.hasUpdatePermission()) return;

        $scope.selectedNodeId = data.id;

        var tenantId = $stateParams.objectId;
        var tenantType = $stateParams.objectTypeId;
        var newBlade = {
            id: 'notificationsList',
            title: data.name,
            tenantId: tenantId,
            tenantType: tenantType,
            subtitle: data.subtitle,
            controller: 'virtoCommerce.notificationsModule.' + data.controllerName,
            template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/' + data.templateName + '.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, blade);
    }

    blade.headIcon = 'fa fa-envelope';

    initializeBlade();

}]);
