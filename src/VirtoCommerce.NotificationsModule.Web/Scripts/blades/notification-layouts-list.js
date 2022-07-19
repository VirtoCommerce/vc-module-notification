angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationLayoutsListController',
        ['$scope', 'virtoCommerce.notificationsModule.notificationsModuleApi',
            'platformWebApp.dialogService', 'platformWebApp.bladeUtils', 'platformWebApp.uiGridHelper', 'platformWebApp.ui-grid.extension',
            function ($scope, notifications, dialogService, bladeUtils, uiGridHelper, gridOptionExtension) {
                var blade = $scope.blade;

                $scope.uiGridConstants = uiGridHelper.uiGridConstants;
                var bladeNavigationService = bladeUtils.bladeNavigationService;

                blade.toolbarCommands = [
                    {
                        name: "platform.commands.add",
                        icon: 'fas fa-plus',
                        executeMethod: createLayout,
                        canExecuteMethod: function () {
                            return true;
                        },
                        permission: 'notifications:create'
                    },
                    {
                        name: "platform.commands.delete",
                        icon: 'fas fa-trash-alt',
                        executeMethod: function () {
                            deleteList($scope.gridApi.selection.getSelectedRows());
                        },
                        canExecuteMethod: function () {
                            return $scope.gridApi && _.any($scope.gridApi.selection.getSelectedRows());
                        },
                        permission: 'notifications:delete'
                    }
                ];

                // filtering
                var filter = $scope.filter = {};

                filter.criteriaChanged = function () {
                    if ($scope.pageSettings.currentPage > 1) {
                        $scope.pageSettings.currentPage = 1;
                    } else {
                        blade.refresh();
                    }
                };

                function getSearchCriteria() {
                    var searchCriteria = {
                        sort: uiGridHelper.getSortExpression($scope),
                        skip: ($scope.pageSettings.currentPage - 1) * $scope.pageSettings.itemsPerPageCount,
                        take: $scope.pageSettings.itemsPerPageCount
                    };

                    return searchCriteria;
                }

                blade.refresh = function () {
                    var searchCriteria = getSearchCriteria();

                    notifications.searchNotificationLayouts(searchCriteria, function (data) {
                        blade.isLoading = false;

                        $scope.pageSettings.totalItems = data.totalCount;
                        $scope.listEntries = data.results ? data.results : [];
                    });
                };

                $scope.selectNode = function (layout) {
                    $scope.selectedNodeId = layout.id;
                    blade.selectedLayout = layout;
                    blade.editNotificationLayout(layout);
                };

                function createLayout() {
                    var newBlade = {
                        id: 'createNotificationLayout',
                        isNew: true,
                        controller: 'virtoCommerce.notificationsModule.notificationLayoutController',
                        template: 'Modules/$(virtoCommerce.Notifications)/Scripts/blades/notification-layout-details.tpl.html'
                    };

                    bladeNavigationService.showBlade(newBlade, blade);
                }


                blade.editNotificationLayout = function (layout) {
                    var newBlade = {
                        id: 'editNotificationLayout',
                        currentEntity: layout,
                        currentEntityId: layout.id,
                        controller: 'virtoCommerce.notificationsModule.notificationLayoutController',
                        template: 'Modules/$(virtoCommerce.Notifications)/Scripts/blades/notification-layout-details.tpl.html'
                    };

                    bladeNavigationService.showBlade(newBlade, blade);
                };

                function deleteList(selection) {
                    var dialog = {
                        id: "confirmDeleteLayouts",
                        title: "notifications.dialogs.notification-layout-delete.title",
                        message: "notifications.dialogs.notification-layout-delete.message",
                        callback: function (remove) {
                            if (remove) {
                                bladeNavigationService.closeChildrenBlades(blade, function () {
                                    var itemIds = _.pluck(selection, 'id');
                                    notifications.deleteNotificationLayout({ ids: itemIds }, function () {
                                        blade.refresh();
                                    },
                                    function (error) {
                                        bladeNavigationService.setError('Error ' + error.status, blade);
                                    });
                                });
                            }
                        }
                    };
                    dialogService.showConfirmationDialog(dialog);
                }

                // ui-grid
                $scope.setGridOptions = function (gridId, gridOptions) {
                    $scope.gridOptions = gridOptions;
                    gridOptionExtension.tryExtendGridOptions(gridId, gridOptions);

                    gridOptions.onRegisterApi = function (gridApi) {
                        $scope.gridApi = gridApi;
                    };

                    bladeUtils.initializePagination($scope);
                };

                blade.headIcon = 'fa fa-list';
            }]);
