angular.module('virtoCommerce.notificationsModule')
    .controller('virtoCommerce.notificationsModule.editTemplateController', ['$rootScope', '$scope', '$timeout', '$localStorage', 'virtoCommerce.notificationsModule.notificationsModuleApi', 'FileUploader', 'platformWebApp.bladeNavigationService', 'platformWebApp.dialogService',
        function ($rootScope, $scope, $timeout, $localStorage, notifications, FileUploader, bladeNavigationService, dialogService) {
            var blade = $scope.blade;
            $scope.isValid = false;

            var formScope;
            $scope.setForm = function (form) {
                formScope = form;
            }

            var codemirrorEditor;
            blade.dynamicProperties = '';

            function saveTemplate() {
                var date = new Date();
                var now = date.getFullYear() + '-' + ('0' + (date.getMonth() + 1)).slice(-2) + '-' + ('0' + date.getDate()).slice(-2);

                if (blade.isNew) {
                    blade.currentEntity.createdDateAsString = now;
                    blade.currentEntity.isReadonly = false;
                    blade.currentEntity.id = blade.currentEntity.languageCode ? null : blade.currentEntity.id;
                    blade.origEntity = angular.copy(blade.currentEntity);
                }
                else {
                    blade.currentEntity.modifiedDateAsString = now;
                    blade.origEntity = angular.copy(blade.currentEntity);
                }

                var sameLanguageTemplate = _.filter(blade.notification.templates, function (template) { return template.languageCode == blade.currentEntity.languageCode; })
                var hasPredefinedTemplates = _.any(sameLanguageTemplate, function (template) { return template.isPredefined });

                if (hasPredefinedTemplates) {
                    blade.currentEntity.isPredefined = true;
                    blade.currentEntity.isEdited = true;
                } else {
                    blade.currentEntity.isPredefined = false;
                }

                var ind = blade.notification.templates.findIndex(function (element) {
                    return (element.languageCode == blade.currentEntity.languageCode)
                        && (element.isPredefined == blade.currentEntity.isPredefined && element.isEdited == blade.currentEntity.isEdited);
                });

                if (ind >= 0) {
                    blade.notification.templates[ind] = blade.currentEntity;
                }
                else {
                    blade.notification.templates.push(blade.currentEntity);
                }
            }

            function restoreTemplate(template) {
                var dialog = {
                    id: "confirmResetTemplates",
                    template: template,
                    callback: function (confirmed) {
                        if (confirmed) {
                            deleteTemplate(template);
                        }
                    }
                }
                dialogService.showDialog(dialog, 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notification-templates-list-reset-dialog.tpl.html', 'platformWebApp.confirmDialogController');
            }

            function deleteTemplate(template) {
                var index = blade.notification.templates.findIndex(function (element) {
                    return (element.languageCode == template.languageCode && element.isPredefined == template.isPredefined && element.isEdited == template.isEdited);
                });

                if (index > -1) {
                    blade.notification.templates.splice(index, 1);
                }

                blade.parentBlade.initialize();
                $scope.bladeClose();
            }

            $scope.saveChanges = function () {
                saveTemplate();
                blade.parentBlade.initialize();
                $scope.bladeClose();
            };

            //todo
            var contentType = 'image';//blade.contentType.substr(0, 1).toUpperCase() + blade.contentType.substr(1, blade.contentType.length - 1);
            $scope.fileUploader = new FileUploader({
                url: 'api/assets?folderUrl=cms-content/' + contentType + '/assets',
                headers: { Accept: 'application/json' },
                autoUpload: true,
                removeAfterUpload: true,
                onBeforeUploadItem: function (fileItem) {
                    blade.isLoading = true;
                },
                onSuccessItem: function (fileItem, response, status, headers) {
                    $scope.$broadcast('filesUploaded', { items: response });
                },
                onErrorItem: function (fileItem, response, status, headers) {
                    bladeNavigationService.setError(fileItem._file.name + ' failed: ' + (response.message ? response.message : status), blade);
                },
                onCompleteAll: function () {
                    blade.isLoading = false;
                }
            });

            function setTemplate() {
                if (!blade.currentEntity) {
                    blade.currentEntity = { kind: blade.notification.kind };
                }

                blade.isLoading = false;
                if (blade.currentEntity && blade.currentEntity.languageCode === undefined) {
                    blade.currentEntity.languageCode = null;
                }

                $timeout(function () {
                    if (codemirrorEditor) {
                        codemirrorEditor.refresh();
                        codemirrorEditor.focus();
                    }
                    blade.origEntity = angular.copy(blade.currentEntity);
                }, 1);

                $scope.isValid = false;
            };

            blade.initialize = function () {
                blade.isLoading = true;
                var found = blade.editedTemplate || _.find(blade.notification.templates, function (templ) { return templ.languageCode === blade.languageCode });
                if (found) {
                    blade.currentEntity = angular.copy(found);
                    blade.origEntity = angular.copy(blade.currentEntity);
                    blade.orightml = blade.currentEntity.body;
                }

                setTemplate();
            };

            blade.renderTemplate = function () {
                var newBlade = {
                    id: 'renderTemplate',
                    title: 'notifications.blades.notifications-template-render.title',
                    subtitle: 'notifications.blades.notifications-template-render.subtitle',
                    subtitleValues: { type: blade.notificationType },
                    notification: blade.notification,
                    tenantId: blade.tenantId,
                    tenantType: blade.tenantType,
                    currentEntity: blade.currentEntity,
                    languageCode: blade.currentEntity.languageCode,
                    controller: 'virtoCommerce.notificationsModule.templateRenderController',
                    template: 'Modules/$(VirtoCommerce.Notifications)/Scripts/blades/notifications-template-render.tpl.html'
                };

                bladeNavigationService.showBlade(newBlade, blade);
            }

            $scope.blade.toolbarCommands = [{
                name: "platform.commands.preview",
                icon: 'fa fa-eye',
                executeMethod: function () {
                    blade.renderTemplate();
                },
                canExecuteMethod: canRender,
                permission: 'notifications:templates:read'
            }, {
                name: "notifications.commands.restore",
                icon: "fa fa-history",
                executeMethod: function () {
                    restoreTemplate(blade.currentEntity);
                },
                canExecuteMethod: function () {
                    return blade.currentEntity.isPredefined && blade.currentEntity.isEdited;
                },
                permission: 'notifications:template:delete'
            }];

            function isDirty() {
                return (!angular.equals(blade.origEntity, blade.currentEntity) || blade.isNew) && blade.hasUpdatePermission();
            }

            function canRender() {
                return !isDirty();
            }

            $scope.$watch("blade.currentEntity", function () {
                $scope.isValid = isDirty() && formScope && formScope.$valid && !blade.origEntity.isReadonly;
            }, true);

            blade.headIcon = 'fa fa-envelope';

            blade.initialize();
        }]);
