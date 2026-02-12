angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationsJournalController', ['$scope', '$translate', 'platformWebApp.bladeNavigationService', 'virtoCommerce.notificationsModule.notificationsModuleApi', 'platformWebApp.bladeUtils', 'platformWebApp.dialogService', 'uiGridConstants', 'platformWebApp.uiGridHelper',
        function ($scope, $translate, bladeNavigationService, notifications, bladeUtils, dialogService, uiGridConstants, uiGridHelper) {
            var blade = $scope.blade;
            $scope.uiGridConstants = uiGridConstants;

            // simple and advanced filtering
            var filter = $scope.filter = {};
            filter.searchInBody = false;
            filter.showPanel = false;
            filter.status = null;
            filter.dateRange = 'last24h';
            filter.startDate = new Date(new Date().getTime() - 24 * 60 * 60 * 1000);
            filter.endDate = null;
            filter.customStartDate = null;
            filter.customEndDate = null;
            filter.customRangeApplied = false;
            filter.showCustomInputs = false;

            filter.statuses = [
                { value: null, label: 'notifications.blades.notifications-journal.labels.filter-status-all' },
                { value: 'Sent', label: 'notifications.blades.notifications-journal.labels.success' },
                { value: 'Pending', label: 'notifications.blades.notifications-journal.labels.processing' },
                { value: 'Error', label: 'notifications.blades.notifications-journal.labels.error' }
            ];

            filter.dateRanges = [
                { value: null, label: 'notifications.blades.notifications-journal.labels.filter-date-any' },
                { value: 'today', label: 'notifications.blades.notifications-journal.labels.filter-date-today' },
                { value: 'yesterday', label: 'notifications.blades.notifications-journal.labels.filter-date-yesterday' },
                { value: 'last24h', label: 'notifications.blades.notifications-journal.labels.filter-date-last24h' },
                { value: 'last7d', label: 'notifications.blades.notifications-journal.labels.filter-date-last7d' },
                { value: 'last30d', label: 'notifications.blades.notifications-journal.labels.filter-date-last30d' },
                { value: 'custom', label: 'notifications.blades.notifications-journal.labels.filter-date-custom' }
            ];

            function startOfDay(date) {
                var d = new Date(date);
                d.setHours(0, 0, 0, 0);
                return d;
            }

            filter.dateRangeChanged = function () {
                filter.customRangeApplied = false;
                filter.showCustomInputs = false;
                var now = new Date();
                var today = startOfDay(now);

                switch (filter.dateRange) {
                    case 'today':
                        filter.startDate = today;
                        filter.endDate = null;
                        break;
                    case 'yesterday':
                        var yesterday = new Date(today);
                        yesterday.setDate(yesterday.getDate() - 1);
                        filter.startDate = yesterday;
                        filter.endDate = today;
                        break;
                    case 'last24h':
                        filter.startDate = new Date(now.getTime() - 24 * 60 * 60 * 1000);
                        filter.endDate = null;
                        break;
                    case 'last7d':
                        var d7 = new Date(today);
                        d7.setDate(d7.getDate() - 7);
                        filter.startDate = d7;
                        filter.endDate = null;
                        break;
                    case 'last30d':
                        var d30 = new Date(today);
                        d30.setDate(d30.getDate() - 30);
                        filter.startDate = d30;
                        filter.endDate = null;
                        break;
                    case 'custom':
                        filter.startDate = null;
                        filter.endDate = null;
                        filter.customStartDate = null;
                        filter.customEndDate = null;
                        filter.showCustomInputs = true;
                        return; // don't refresh yet
                    default:
                        filter.startDate = null;
                        filter.endDate = null;
                        break;
                }
                filter.criteriaChanged();
            };

            filter.applyCustomRange = function () {
                filter.startDate = filter.customStartDate || null;
                filter.endDate = filter.customEndDate || null;
                filter.customRangeApplied = true;
                filter.showCustomInputs = false;
                filter.criteriaChanged();
            };

            filter.editCustomRange = function () {
                filter.showCustomInputs = true;
                filter.customRangeApplied = false;
            };

            filter.formatDate = function (date) {
                if (!date) return '';
                var d = new Date(date);
                return d.toLocaleDateString();
            };

            filter.togglePanel = function ($event) {
                $event.stopPropagation();
                filter.showPanel = !filter.showPanel;
            };

            filter.hasActiveFilters = function () {
                return filter.searchInBody || filter.startDate || filter.endDate || filter.status;
            };

            filter.clearFilters = function () {
                filter.searchInBody = false;
                filter.startDate = null;
                filter.endDate = null;
                filter.status = null;
                filter.dateRange = null;
                filter.customStartDate = null;
                filter.customEndDate = null;
                filter.customRangeApplied = false;
                filter.showCustomInputs = false;
                filter.criteriaChanged();
            };

            // close panel on outside click
            var closePanel = function () {
                if (filter.showPanel) {
                    filter.showPanel = false;
                    $scope.$apply();
                }
            };
            angular.element(document).on('click', closePanel);
            $scope.$on('$destroy', function () {
                angular.element(document).off('click', closePanel);
            });

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
                    startDate: filter.startDate || undefined,
                    endDate: filter.endDate || undefined,
                    status: filter.status || undefined,
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
