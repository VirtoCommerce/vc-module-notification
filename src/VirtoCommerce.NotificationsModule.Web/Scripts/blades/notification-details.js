angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.notificationsEditController', ['$scope', '$filter', 'virtoCommerce.notificationsModule.notificationsModuleApi', 'platformWebApp.bladeNavigationService',
        function ($scope, $filter, notifications, bladeNavigationService) {
            $scope.setForm = function (form) {
                $scope.formScope = form;
            }

            var blade = $scope.blade;
            blade.updatePermission = 'notifications:update';
            var codemirrorEditor;
            blade.parametersForTemplate = [];
            $scope.isValid = false;

            blade.initialize = function () {
                blade.isLoading = true;
                notifications.getNotificationByType({ type: blade.type, tenantId: blade.tenantId, tenantType: blade.tenantType }, function (data) {
                    blade.isLoading = false;
                    setNotification(data);
                })
            };

            function modifyEmailAddress(addresses) {
                return _.map(addresses, function (address) { return { value: address }; });
            }

            function setNotification(data) {
                blade.currentEntity = angular.copy(data);

                if (isTransient(blade.currentEntity)) {
                    blade.currentEntity.tenantIdentity = { id: blade.tenantId, type: blade.tenantType };
                }

                blade.currentEntity.cc = modifyEmailAddress(blade.currentEntity.cc);
                blade.currentEntity.bcc = modifyEmailAddress(blade.currentEntity.bcc);
                _.map(blade.currentEntity.templates, function (template) {
                    template.createdDateAsString = $filter('date')(template.createdDate, "yyyy-MM-dd");
                    template.modifiedDateAsString = $filter('date')(template.modifiedDate, "yyyy-MM-dd");
                    return template;
                });
                if (!blade.currentEntity.templates) blade.currentEntity.templates = [];

                blade.origEntity = angular.copy(blade.currentEntity);
                $scope.isValid = false;
            };

            function isTransient(data) {
                return data.tenantIdentity.isEmpty && blade.tenantId && blade.tenantType;
            }

            function pluckAddress(address) {
                if (address) {
                    return _(address).pluck('value');
                }
                return address;
            }

            blade.updateNotification = function () {
                blade.isLoading = true;
                blade.currentEntity.cc = pluckAddress(blade.currentEntity.cc);
                blade.currentEntity.bcc = pluckAddress(blade.currentEntity.bcc);

                var entityToSave = angular.copy(blade.currentEntity);
                entityToSave.templates = _.filter(blade.currentEntity.templates, { isReadonly: false });
                entityToSave.templates.forEach(element => {
                    // Need to set IsPredefined to false in order to save the template to the database
                    if (!!element.isEdited && !!element.isPredefined) {
                        element.isPredefined = false;
                    }
                });

                notifications.updateNotification({ type: blade.type }, entityToSave, function () {
                    blade.isLoading = false;
                    blade.origEntity = angular.copy(blade.currentEntity);
                    blade.parentBlade.refresh();
                    bladeNavigationService.closeBlade(blade);
                });
            };

            $scope.blade.toolbarCommands = [
                {
                    name: "platform.commands.save", icon: 'fas fa-save',
                    executeMethod: blade.updateNotification,
                    canExecuteMethod: canSave,
                    permission: blade.updatePermission
                },
                {
                    name: "platform.commands.undo", icon: 'fa fa-undo',
                    executeMethod: function () {
                        blade.currentEntity = angular.copy(blade.origEntity);
                    },
                    canExecuteMethod: isDirty,
                    permission: blade.updatePermission
                }
            ];

            $scope.editorOptions = {
                lineWrapping: true,
                lineNumbers: true,
                parserfile: "liquid.js",
                extraKeys: { "Ctrl-Q": function (cm) { cm.foldCode(cm.getCursor()); } },
                foldGutter: true,
                gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"],
                onLoad: function (_editor) {
                    codemirrorEditor = _editor;
                },
                mode: "liquid-html"
            };

            $scope.$watch("blade.currentEntity", function () {
                $scope.isValid = $scope.formScope && $scope.formScope.$valid;
            }, true);

            function isDirty() {
                return (!angular.equals(blade.origEntity, blade.currentEntity) || blade.isNew) && blade.hasUpdatePermission();
            }

            function canSave() {
                return isDirty() && $scope.isValid;
            }

            blade.onClose = function (closeCallback) {
                bladeNavigationService.showConfirmationIfNeeded(isDirty(), canSave(), blade, blade.updateNotification, closeCallback, "notifications.dialogs.notification-details-save.title", "notifications.dialogs.notification-details-save.message");
            };

            blade.headIcon = 'fa fa-envelope';

            blade.initialize();
        }]);
