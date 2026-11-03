angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationLayoutPreviewController',
        ['$scope', '$sce', '$http', '$timeout', 'virtoCommerce.notificationsModule.notificationsModuleApi',
            function ($scope, $sce, $http, $timeout, notifications) {
                var blade = $scope.blade;
                var layoutBlade = blade.layoutBlade;
                var renderTimer;

                blade.headIcon = 'fa fa-eye';
                blade.isLoading = true;
                blade.emailTypes = [];
                blade.stores = [];
                blade.languages = [];
                blade.selection = { type: null, storeId: null, language: null };

                // The preview renders the sample of an email type inside the layout text being edited, so changes
                // show before the layout is saved. The brand comes from the selected store.
                function initialize() {
                    notifications.getNotificationList({ take: 1000 }, function (result) {
                        blade.emailTypes = _.chain(result.results)
                            .filter(function (x) { return x.kind === 'EmailNotification'; })
                            .pluck('type')
                            .sortBy()
                            .value();

                        var usedTypes = layoutBlade.usage ? layoutBlade.usage.notificationTypes : [];
                        blade.selection.type = _.find(usedTypes, function (x) { return _.contains(blade.emailTypes, x); }) || blade.emailTypes[0];
                        loadNotification();
                    }, onError);

                    // The store list comes from the Store module; without it the preview uses the global brand
                    $http.post('api/stores/search', { take: 1000, responseGroup: 'StoreInfo' }).then(function (response) {
                        blade.stores = response.data.results || [];
                    }, function () {
                        blade.stores = [];
                    });
                }

                function loadNotification() {
                    if (!blade.selection.type) {
                        blade.isLoading = false;
                        return;
                    }

                    blade.isLoading = true;
                    notifications.getNotificationByType({ type: blade.selection.type }, function (notification) {
                        blade.notification = notification;
                        var emailTemplates = _.filter(notification.templates || [], function (x) { return x.kind === 'EmailNotification'; });
                        blade.languages = _.chain(emailTemplates).pluck('languageCode').uniq().value();
                        if (!_.contains(blade.languages, blade.selection.language)) {
                            blade.selection.language = blade.languages[0];
                        }
                        render();
                    }, onError);
                }

                // Among the templates of the selected language prefer the one built on this layout, then the DB one
                // (it wins over the predefined one when the email is sent)
                function findTemplate() {
                    var layout = layoutBlade.currentEntity;
                    var templates = _.filter(blade.notification.templates || [], function (x) { return x.kind === 'EmailNotification'; });
                    var candidates = _.filter(templates, function (x) { return x.languageCode === blade.selection.language; });
                    if (!candidates.length) {
                        candidates = _.filter(templates, function (x) { return !x.languageCode; });
                    }
                    return _.find(candidates, function (x) { return x.notificationLayoutId && (x.notificationLayoutId === layout.id || x.notificationLayoutId === layout.name); })
                        || _.find(candidates, function (x) { return !x.isPredefined; })
                        || candidates[0];
                }

                function render() {
                    var template = blade.notification && findTemplate();
                    if (!template) {
                        blade.previewHtml = null;
                        blade.noTemplate = true;
                        blade.isLoading = false;
                        return;
                    }

                    blade.noTemplate = false;
                    var data = angular.copy(blade.notification);
                    if (template.sample) {
                        try {
                            angular.extend(data, JSON.parse(template.sample));
                        } catch (e) {
                            // An invalid sample renders with the notification as is
                        }
                    }

                    blade.isLoading = true;
                    notifications.renderTemplate({
                        type: blade.notification.type,
                        language: blade.selection.language || 'default'
                    }, {
                        text: template.body,
                        data: data,
                        layoutTemplate: layoutBlade.currentEntity.template || '{{ content }}',
                        storeId: blade.selection.storeId
                    }, function (response) {
                        blade.error = null;
                        blade.previewHtml = $sce.trustAsHtml(response.html);
                        blade.isLoading = false;
                    }, onError);
                }

                function onError(error) {
                    blade.error = error;
                    blade.isLoading = false;
                }

                $scope.onTypeChanged = loadNotification;
                $scope.onOptionsChanged = render;

                // Re-render while the layout text is edited in the parent blade
                $scope.$watch(function () { return layoutBlade.currentEntity && layoutBlade.currentEntity.template; }, function (newValue, oldValue) {
                    if (newValue === oldValue || !blade.notification) {
                        return;
                    }
                    $timeout.cancel(renderTimer);
                    renderTimer = $timeout(render, 600);
                });

                $scope.$on('$destroy', function () {
                    $timeout.cancel(renderTimer);
                });

                blade.toolbarCommands = [{
                    name: 'platform.commands.refresh',
                    icon: 'fa fa-refresh',
                    executeMethod: render,
                    canExecuteMethod: function () { return !!blade.notification; }
                }];

                initialize();
            }]);
