angular.module('virtoCommerce.notificationsModule')
.controller('virtoCommerce.notificationsModule.notificationsTemplatesWidgetController', ['$scope', '$translate', 'platformWebApp.bladeNavigationService', function ($scope, $translate, bladeNavigationService) {
	var blade = $scope.widget.blade;
    $scope.templatesCount = '...';
    
    $scope.$watch('blade.currentEntity', function (notification) {
        if (notification && notification.templates)
            $scope.templatesCount = notification.templates.length;
    });

	blade.showTemplates = function () {
		var newBlade = {
			id: 'notificationTemplatesWidgetChild',
			title: 'notifications.widgets.notificationsTemplatesWidget.blade-title',
			titleValues: { displayName: $translate.instant('notificationTypes.' + blade.currentEntity.type + '.displayName') },
			tenantId: blade.currentEntity.tenantId,
			tenantType: blade.currentEntity.tenantType,
            currentEntity : blade.currentEntity,
            subtitle: 'notifications.widgets.notificationsTemplatesWidget.blade-subtitle',
			controller: 'virtoCommerce.notificationsModule.notificationTemplatesListController',
			template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-templates-list.tpl.html'
		};
		bladeNavigationService.showBlade(newBlade, blade);
	};
}]);
