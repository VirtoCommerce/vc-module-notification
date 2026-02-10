angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationsJournalController', ['$scope', '$translate', 'platformWebApp.bladeNavigationService', 'virtoCommerce.notificationsModule.notificationsModuleApi', 'platformWebApp.bladeUtils', 'platformWebApp.dialogService', 'uiGridConstants', 'platformWebApp.uiGridHelper',
        function ($scope, $translate, bladeNavigationService, notifications, bladeUtils, dialogService, uiGridConstants, uiGridHelper) {
            var blade = $scope.blade;
            $scope.uiGridConstants = uiGridConstants;

            // simple and advanced filtering
            var filter = $scope.filter = {};
            filter.searchInBody = false;

            filter.criteriaChanged = function () {
                if ($scope.pageSettings.currentPage > 1) {
                    $scope.pageSettings.currentPage = 1;
                } else {
                    blade.refresh();
                }
            };

            // Search Criteria
            function getSearchCriteria() {
                var searchCriteria = {
                    objectIds: blade.objectId ? [blade.objectId] : undefined,
                    objectType: blade.objectType,
                    keyword: filter.keyword ? filter.keyword : undefined,
                    searchInBody: filter.searchInBody || undefined,
                    sort: uiGridHelper.getSortExpression($scope),
                    skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                    take: $scope.pageSettings.itemsPerPageCount,
                    notificationType: blade.notificationType,
                };
                return searchCriteria;
            }

            blade.refresh = function () {
                blade.isLoading = true;

                var searchCriteria = getSearchCriteria();
                notifications.getNotificationJournalList(searchCriteria, function (data) {
                    blade.currentEntities = data.results;
                    $scope.pageSettings.totalItems = data.totalCount;
                    blade.isLoading = false;
                });
            };

            $scope.selectNode = function (data) {
                $scope.selectedNodeId = data.id;

                var newBlade = {
                    id: 'notificationDetails',
                    title: data.subject,
                    subtitle: 'notifications.blades.notification-journal-details.subtitle',
                    subtitleValues: { displayName: $translate.instant(data.displayName) },
                    currentNotificationId: data.id,
                    currentEntity: data,
                    controller: 'virtoCommerce.notificationsModule.notificationJournalDetailsController',
                    template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-journal-details.tpl.html'
                };
                bladeNavigationService.showBlade(newBlade, blade);
            };

            $scope.stopNotifications = function (list) {
                blade.isLoading = true;
                notifications.stopSendingNotifications(_.pluck(list, 'id'), blade.refresh);
            };

            blade.toolbarCommands = [
                {
                    name: "platform.commands.refresh", icon: 'fa fa-refresh',
                    executeMethod: blade.refresh,
                    canExecuteMethod: function () { return true; }
                },
                {
                    name: 'notifications.blades.notifications-journal.labels.resend-messages',
                    icon: 'fa fa-repeat',
                    executeMethod: function () {
                        var messageIds = blade.$scope.gridApi.grid.rows
                            .filter(x => x.isSelected)
                            .map(x => x.entity.id);

                        notifications.resendNotifications(messageIds, function () {
                            blade.refresh();
                        });
                    },
                    canExecuteMethod: function () {
                        return blade.$scope.gridApi !== undefined &&
                            blade.$scope.gridApi.grid.rows.filter(x => x.isSelected).length > 0;
                    },
                    permission: 'notifications:update'
                }
            ];



            // ui-grid
            $scope.setGridOptions = function (gridOptions) {
                uiGridHelper.initialize($scope, gridOptions, function (gridApi) {
                    //update gridApi for current grid
                    $scope.gridApi = gridApi;
                    uiGridHelper.bindRefreshOnSortChanged($scope);
                });
                bladeUtils.initializePagination($scope);
            };
        }]);
